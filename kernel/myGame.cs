using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using CoreRunManager = MegaCrit.Sts2.Core.Runs.RunManager;

namespace MegaCrit.Sts2.Core;

/*
This is the kernel-owned game shell and runtime state coordinator.

This version moves one step closer to the target architecture:
1. all user-facing text output goes through InteractionChannel
2. all input polling also goes through InteractionChannel
3. myGame owns the high-level run / map / room state machine
4. myRunManager remains only as a content-generation helper for room pools and act maps

Combat execution and full event execution are still not fully bridged into the
pure-kernel runtime yet. This version adds a thin command-driven bridge:
1. myEventExecutionBridge can execute legacy event logic without GUI
2. myCombatSession lets myGame own combat/event transitions even before combat simulation exists

That keeps room flow ownership in myGame while still leaving the deeper combat
engine migration for later steps.
*/

public class myGame
{
	public enum InteractionChannel
	{
		CommandLine,
		BinaryPipe
	}

	public enum GameState
	{
		Uninitialized,
		CharacterSelection,
		RunSetup,
		OnMap,
		EnteringRoom,
		InRoom,
		InCombat,
		ResolvingRoom,
		TransitioningAct,
		RunEnded
	}

	public enum RoomState
	{
		None,
		Created,
		Entered,
		Active,
		Completed
	}

	private static readonly HashSet<string> _exitCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"exit",
		"quit"
	};

	private enum ActionKind
	{
		EventOption,
		TreasureOpen,
		RestHeal,
		CompleteRoom,
		TakeReward,
		SkipReward
	}

	private sealed class ActionChoice
	{
		public ActionKind Kind { get; init; }

		public string Label { get; init; } = string.Empty;

		public int PrimaryIndex { get; init; } = -1;

		public int SecondaryIndex { get; init; } = -1;
	}

	private sealed class NewRunRequestedException : Exception
	{
		public string? Seed { get; }

		public NewRunRequestedException(string? seed = null)
		{
			Seed = seed;
		}
	}

	private enum CombatPromptKind
	{
		Card,
		Potion,
		EndTurn
	}

	private sealed class CombatPromptAction
	{
		public CombatPromptKind Kind { get; init; }

		public int Index { get; init; }
	}

	private enum RewardPromptKind
	{
		TakeReward,
		SkipAll
	}

	private sealed class RewardPromptAction
	{
		public RewardPromptKind Kind { get; init; }

		public int RewardIndex { get; init; }
	}

	private enum ShopPromptKind
	{
		Purchase,
		Leave
	}

	private sealed class ShopPromptAction
	{
		public ShopPromptKind Kind { get; init; }

		public MerchantEntry? Entry { get; init; }
	}

	private readonly Action<string> _writer;

	private readonly Action<string> _promptWriter;

	private readonly Func<string?> _reader;

	private readonly myCliInteraction _cli;

	private myBinaryPipeHandler? _pipe;

	private const ulong _localPlayerNetId = 1uL;

	private bool _interactiveExitRequested;
	private bool _globalCommandBreak;

	public UnlockState AvailableUnlockState { get; }

	public GameState State { get; private set; } = GameState.Uninitialized;

	public RoomState CurrentRoomState { get; private set; } = RoomState.None;

	public CharacterModel? SelectedCharacterModel { get; private set; }

	public int SelectedAscensionLevel { get; private set; }

	public RunRngSet? RunRng { get; private set; }

	public RunState? RunState { get; private set; }

	public myRunManager? RunManager { get; private set; }

	public ActMap? MapInstance { get; private set; }

	public AbstractRoom? CurrentRoom => RunState?.CurrentRoom;

	public myCombatSession? CombatSession { get; private set; }

	public myEventExecutionBridge? EventBridge { get; private set; }

	public myPendingRewardState? PendingRewards { get; private set; }

	public string Seed { get; private set; } = string.Empty;

	private bool _treasureOpened;

	private bool _restActionTaken;

	private bool _replaceTreasureWithElites;

	public myGame(UnlockState? unlockState = null, Action<string>? writer = null, Func<string?>? reader = null, Action<string>? promptWriter = null, Action<string>? auditWriter = null, myBinaryPipeHandler? pipe = null)
	{
		EnsureInitialized();
		AvailableUnlockState = unlockState ?? UnlockState.all;
		_writer = writer ?? Console.WriteLine;
		_reader = reader ?? Console.ReadLine;
		_promptWriter = promptWriter ?? Console.Write;
		_pipe = pipe;
		_cli = new myCliInteraction(_writer, _reader, _promptWriter, auditWriter);
		if (_pipe != null)
		{
			_cli.SetPipeHandler(_pipe);
		}
		myHeadlessRewardRuntime.OfferedRewardsSink = state => PendingRewards = state;
	}

	public void RunInteractiveLoop(string? seed = null, bool replaceTreasureWithElites = false, InteractionChannel channel = InteractionChannel.CommandLine)
	{
		EnsureInitialized();
		_interactiveExitRequested = false;
		_replaceTreasureWithElites = replaceTreasureWithElites;
		showImportant("sts2kernel command line mode", channel);
		if (!BeginRunSession(seed, allowLoadPrompt: true, channel))
		{
			return;
		}
		ShowRunInitialized(channel);
		while (State != GameState.RunEnded && !_interactiveExitRequested)
		{
			try
			{
				PromptForCurrentState(channel);
			}
			catch (NewRunRequestedException ex)
			{
				if (!BeginRunSession(ex.Seed, allowLoadPrompt: false, channel))
				{
					return;
				}
				ShowRunInitialized(channel);
			}
			catch (OperationCanceledException)
			{
				return;
			}
		}
	}

	private void StartOrLoadRun(string? seed, bool replaceTreasureWithElites, InteractionChannel channel)
	{
		if (!SaveManager.Instance.HasRunSave)
		{
			StartNewRun(seed, replaceTreasureWithElites, channel);
			return;
		}
		while (true)
		{
			myCliChoice? choice = PromptChoice("Choose run: ", new[]
			{
				new myCliChoice
				{
					Key = "1",
					Index = 1,
					Text = "new game",
					Payload = false
				},
				new myCliChoice
				{
					Key = "2",
					Index = 2,
					Text = "load game",
					Payload = true
				}
			}, channel);
			if (choice == null)
			{
				throw new OperationCanceledException("Interactive input ended before the run mode was selected.");
			}
			if (choice.Payload is not true)
			{
				StartNewRun(seed, replaceTreasureWithElites, channel);
				return;
			}
			ReadSaveResult<SerializableRun> saveResult = SaveManager.Instance.LoadRunSave();
			if (saveResult.Success && saveResult.SaveData != null)
			{
				LoadSavedRun(saveResult.SaveData, replaceTreasureWithElites, channel);
				return;
			}
			WriteLine($"Failed to load current_run.save: {saveResult.Status}.", channel);
			if (!string.IsNullOrWhiteSpace(saveResult.ErrorMessage))
			{
				WriteLine(saveResult.ErrorMessage, channel);
			}
		}
	}

	private void StartNewRun(string? seed, bool replaceTreasureWithElites, InteractionChannel channel)
	{
		getCharacter(seed, channel);
		getMap(replaceTreasureWithElites, showAvailablePaths: false, channel: channel);
		EnterStartingPoint(channel);
	}

	private bool BeginRunSession(string? seed, bool allowLoadPrompt, InteractionChannel channel)
	{
		while (true)
		{
			try
			{
				if (allowLoadPrompt)
				{
					StartOrLoadRun(seed, _replaceTreasureWithElites, channel);
				}
				else
				{
					StartNewRun(seed, _replaceTreasureWithElites, channel);
				}
				return true;
			}
			catch (NewRunRequestedException ex)
			{
				seed = ex.Seed;
				allowLoadPrompt = false;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
		}
	}

	private void ShowRunInitialized(InteractionChannel channel)
	{
		showImportant("Run initialized. Enter the shown option number to act. Global queries: m/map, c/deck, hp, g, combat, help, new. Type exit to quit.", channel);
		showOtherInfo("state", channel);
	}

	private void LoadSavedRun(SerializableRun save, bool replaceTreasureWithElites, InteractionChannel channel)
	{
		LoadSavedRunCore(save, replaceTreasureWithElites, channel);
		ShowCurrentPrompt(channel);
	}

	private void LoadSavedRunSilent(SerializableRun save, bool replaceTreasureWithElites, InteractionChannel channel)
	{
		LoadSavedRunCore(save, replaceTreasureWithElites, channel);
	}

	private void LoadSavedRunCore(SerializableRun save, bool replaceTreasureWithElites, InteractionChannel channel)
	{
		EnsureInitialized();
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		RunState loadedRunState = RunState.FromSerializable(save);
		Player player = loadedRunState.Players.FirstOrDefault() ?? throw new InvalidOperationException("Save file does not contain a player.");
		LocalContext.NetId = player.NetId;
		Seed = loadedRunState.Rng.StringSeed;
		RunRng = loadedRunState.Rng;
		SelectedCharacterModel = player.Character;
		SelectedAscensionLevel = loadedRunState.AscensionLevel;
		RunState = loadedRunState;
		RunManager = new myRunManager(loadedRunState);
		ApplySavedMapsToLoad(RunManager, save);
		MapInstance = null;
		CombatSession = null;
		EventBridge = null;
		PendingRewards = null;
		_treasureOpened = false;
		_restActionTaken = false;
		CurrentRoomState = RoomState.None;
		State = GameState.RunSetup;
		PrepareLoadedHeadlessRuntime(loadedRunState, save);
		GenerateCurrentActMap(replaceTreasureWithElites);
		RestoreLoadedLocation(save, channel);
		showImportant($"Loaded {GetCharacterName(player.Character)} with seed {Seed}.", channel);
	}

	public void showOption(IEnumerable<string> options, InteractionChannel channel = InteractionChannel.CommandLine)
	{
		_cli.ShowOptions(options, channel);
	}

	public int getOption(InteractionChannel channel = InteractionChannel.CommandLine)
	{
		myCliChoice? choice = _cli.GetChoice(channel, (string input) => TryHandleGlobalQuery(input, channel), IsExitCommand);
		if (choice == null)
		{
			throw new OperationCanceledException("Interactive input ended before an option was selected.");
		}
		return choice.Index;
	}

	public void showImportant(string message, InteractionChannel channel = InteractionChannel.CommandLine)
	{
		_cli.WriteImportant(message, channel);
	}

	public void showOtherInfo(string? message = null, InteractionChannel channel = InteractionChannel.CommandLine)
	{
		try
		{
			HandleCommand(message, channel);
		}
		catch (Exception ex)
		{
			WriteLine($"Error: {ex.Message}", channel);
		}
	}

	public CharacterModel getCharacter(string? seed = null, InteractionChannel channel = InteractionChannel.CommandLine)
	{
		EnsureInitialized();
		State = GameState.CharacterSelection;
		List<CharacterModel> characters = AvailableUnlockState.Characters.ToList();
		if (characters.Count == 0)
		{
			throw new InvalidOperationException("No selectable characters are available.");
		}
		showImportant("Choose a character:", channel);
		showOption(characters.Select(BuildCharacterOption), channel);
		int selectedIndex = getOption(channel) - 1;
		CharacterModel selectedCharacter = characters[selectedIndex];
		int selectedAscensionLevel = ResolveNewRunAscension(channel);
		string resolvedSeed = ResolveNewRunSeed(seed, channel);
		Seed = resolvedSeed;
		RunRng = new RunRngSet(Seed);
		SelectedCharacterModel = selectedCharacter;
		SelectedAscensionLevel = selectedAscensionLevel;
		LocalContext.NetId = _localPlayerNetId;
		RunState = null;
		RunManager = null;
		MapInstance = null;
		CombatSession = null;
		EventBridge = null;
		PendingRewards = null;
		_treasureOpened = false;
		_restActionTaken = false;
		CurrentRoomState = RoomState.None;
		State = GameState.RunSetup;
		showImportant($"Selected {GetCharacterName(selectedCharacter)} | Ascension {SelectedAscensionLevel} | Seed {Seed}.", channel);
		showImportant(BuildCharacterSummary(selectedCharacter), channel);
		return selectedCharacter;
	}

	public ActMap getMap(bool replaceTreasureWithElites = false, bool showAvailablePaths = true, InteractionChannel channel = InteractionChannel.CommandLine)
	{
		EnsureInitialized();
		if (MapInstance != null)
		{
			return MapInstance;
		}
		State = GameState.RunSetup;
		CharacterModel character = SelectedCharacterModel ?? getCharacter(channel: channel);
		Player player = Player.CreateForNewRun(character, AvailableUnlockState, _localPlayerNetId);
		List<ActModel> acts = ActModel.GetDefaultList().Select((ActModel act) => act.ToMutable()).ToList();
		RunState runState = RunState.CreateForNewRun(new[] { player }, acts, Array.Empty<ModifierModel>(), GameMode.Standard, SelectedAscensionLevel, Seed);
		LocalContext.NetId = player.NetId;
		RunState = runState;
		RunManager = new myRunManager(runState);
		RunRng = runState.Rng;
		PrepareNewHeadlessRuntime(runState);
		GenerateCurrentActMap(replaceTreasureWithElites);
		CombatSession = null;
		EventBridge = null;
		PendingRewards = null;
		_treasureOpened = false;
		_restActionTaken = false;
		CurrentRoomState = RoomState.None;
		State = GameState.OnMap;
		SaveManager.Instance.SaveRun(null);
		showImportant($"Generated map for act {GetActName(runState.Act.SourceAct)}.", channel);
		if (showAvailablePaths)
		{
			ShowAvailablePaths(channel);
		}
		return MapInstance;
	}

	public IReadOnlyList<MapPoint> GetAvailableMapPoints()
	{
		if (RunState == null || MapInstance == null)
		{
			return Array.Empty<MapPoint>();
		}
		if (CurrentRoom != null && CurrentRoomState != RoomState.None && CurrentRoomState != RoomState.Completed)
		{
			return Array.Empty<MapPoint>();
		}
		IEnumerable<MapPoint> points = RunState.VisitedMapCoords.Count == 0
			? new[] { MapInstance.StartingMapPoint }
			: RunState.CurrentMapPoint?.Children ?? new HashSet<MapPoint>();
		return points
			.Where((MapPoint point) => !RunState.VisitedMapCoords.Contains(point.coord))
			.OrderBy((MapPoint point) => point.coord.row)
			.ThenBy((MapPoint point) => point.coord.col)
			.ToList();
	}

	public AbstractRoom EnterStartingPoint(InteractionChannel channel = InteractionChannel.CommandLine)
	{
		ActMap map = RequireMap();
		if (RunState?.VisitedMapCoords.Count > 0)
		{
			throw new InvalidOperationException("The run has already left the starting point. Use 'next' or 'go <index>' instead.");
		}
		return EnterMapCoord(map.StartingMapPoint.coord, channel);
	}

	public AbstractRoom EnterMapCoord(MapCoord coord, InteractionChannel channel = InteractionChannel.CommandLine)
	{
		RunState runState = RequireRunState();
		ActMap map = RequireMap();
		MapPoint point = map.GetPoint(coord) ?? throw new InvalidOperationException($"Map does not contain coord {coord}.");
		IReadOnlyList<MapPoint> availablePoints = GetAvailableMapPoints();
		if (!availablePoints.Any((MapPoint availablePoint) => availablePoint.coord == coord))
		{
			throw new InvalidOperationException($"Map coord {coord} is not currently reachable.");
		}
		if (!runState.AddVisitedMapCoord(coord))
		{
			throw new InvalidOperationException($"Map coord {coord} has already been visited.");
		}
		UpdateCurrentMapPointHistoryPlayerStats();
		SaveManager.Instance.SaveRun(null);
		State = GameState.EnteringRoom;
		runState.ActFloor = coord.row + 1;
		RoomType roomType = RollRoomTypeFor(runState, point.PointType, myRunManager.BuildRoomTypeBlacklist(runState.CurrentMapPointHistoryEntry, runState.CurrentMapPoint?.Children ?? new HashSet<MapPoint>()));
		AbstractRoom room = CreateRoom(runState, roomType, point.PointType);
		runState.AppendToMapPointHistory(point.PointType, room.RoomType, room.ModelId);
		runState.ClearRoomStack();
		runState.PushRoom(room);
		CombatSession = null;
		EventBridge = null;
		PendingRewards = null;
		_treasureOpened = false;
		_restActionTaken = false;
		CurrentRoomState = RoomState.Created;
		if (room is not CombatRoom && room is not EventRoom)
		{
			room.Enter(runState, isRestoringRoomStackBase: false);
		}
		CurrentRoomState = RoomState.Entered;
		CurrentRoomState = RoomState.Active;
		showImportant($"Entered {BuildRoomLabel(room)} at ({coord.col}, {coord.row}).", channel);
		switch (room)
		{
		case EventRoom eventRoom:
			BeginEventRoom(eventRoom, channel);
			break;
		case CombatRoom combatRoom:
			BeginCombatSession(myCombatSession.CreateForRoom(combatRoom), channel);
			break;
		default:
			State = GameState.InRoom;
			WriteLine(BuildRoomSummary(room), channel);
			break;
		}
		return room;
	}

	public void CompleteCurrentRoom(InteractionChannel channel = InteractionChannel.CommandLine)
	{
		RunState runState = RequireRunState();
		if (CombatSession != null)
		{
			throw new InvalidOperationException("Combat is still active. Use play <handIndex> [targetIndex], potion <slotIndex> [targetIndex], or e to continue the fight.");
		}
		if (PendingRewards != null && !PendingRewards.IsEmpty)
		{
			throw new InvalidOperationException("There are pending rewards. Resolve them before leaving the room.");
		}
		if (EventBridge != null && !EventBridge.IsFinished)
		{
			throw new InvalidOperationException("The current event is not finished yet. Choose an option first.");
		}
		AbstractRoom room = CurrentRoom ?? throw new InvalidOperationException("There is no active room to complete.");
		State = GameState.ResolvingRoom;
		if (EventBridge != null)
		{
			EventBridge.EnsureCleanup();
			EventBridge = null;
		}
		room.Exit(runState);
		runState.Act.MarkRoomVisited(room.RoomType);
		CurrentRoomState = RoomState.Completed;
		runState.ClearRoomStack();
		CombatSession = null;
		PendingRewards = null;
		_treasureOpened = false;
		_restActionTaken = false;
		WriteLine($"Completed {BuildRoomLabel(room)}.", channel);
		if (room.RoomType == RoomType.Boss && !GetAvailableMapPoints().Any())
		{
			if (runState.CurrentActIndex >= runState.Acts.Count - 1)
			{
				State = GameState.RunEnded;
				CurrentRoomState = RoomState.None;
				showImportant("Run complete.", channel);
				return;
			}
			AdvanceToNextAct(channel);
			return;
		}
		State = GameState.OnMap;
		CurrentRoomState = RoomState.None;
	}

	private static void EnsureInitialized()
	{
		OneTimeInitialization.ExecuteEssential();
	}

	private static string NormalizeSeed(string? seed)
	{
		string resolvedSeed = string.IsNullOrWhiteSpace(seed) ? SeedHelper.GetRandomSeed() : seed;
		return SeedHelper.CanonicalizeSeed(resolvedSeed);
	}

	private string ResolveNewRunSeed(string? seed, InteractionChannel channel)
	{
		if (!string.IsNullOrWhiteSpace(seed))
		{
			return NormalizeSeed(seed);
		}

		string? input = ReadInput("Enter seed (leave blank for random): ", channel);
		if (input == null)
		{
			throw new OperationCanceledException("Interactive input ended before the seed was entered.");
		}

		string trimmed = input.Trim().TrimStart('\uFEFF');
		return trimmed.Length == 0 ? NormalizeSeed(null) : NormalizeSeed(trimmed);
	}

	private int ResolveNewRunAscension(InteractionChannel channel)
	{
		showImportant("Choose ascension level:", channel);
		List<myCliChoice> choices = Enumerable.Range(0, 11).Select((int level) => new myCliChoice
		{
			Key = level.ToString(),
			Index = level,
			Text = $"Ascension {level}",
			Payload = level
		}).ToList();
		myCliChoice? choice = PromptChoice("Choose ascension (0-10): ", choices, channel);
		if (choice?.Payload is not int ascensionLevel)
		{
			throw new OperationCanceledException("Interactive input ended before the ascension level was selected.");
		}
		return ascensionLevel;
	}

	private void HandleCommand(string? message, InteractionChannel channel)
	{
		string? command = message?.Trim();
		if (command == null)
		{
			WriteLine("null", channel);
			return;
		}
		if (command.Length == 0)
		{
			WriteLine("Invalid input.", channel);
			return;
		}
		if (TryHandleTravelCommand(command, channel))
		{
			return;
		}
		if (TryHandleChooseCommand(command, channel))
		{
			return;
		}
		if (TryHandleActionCommand(command, channel))
		{
			return;
		}
		if (TryHandleCombatCommand(command, channel))
		{
			return;
		}
		switch (command.ToLowerInvariant())
		{
		case "help":
			ShowHelp(channel);
			return;
		case "m":
		case "map":
			WriteNullable(RenderMap(MapInstance, RunState, GetAvailableMapPoints()), channel);
			return;
		case "c":
			WriteNullable(RenderCardPile(GetDeckCards()), channel);
			return;
		case "hp":
			WriteNullable(GetPlayerHpText(), channel);
			return;
		case "g":
			WriteNullable(GetPlayerGoldText(), channel);
			return;
		case "a":
			WriteNullable(RenderCardPile(GetPile(PileType.Draw)), channel);
			return;
		case "q":
			WriteNullable(RenderCardPile(GetPile(PileType.Discard)), channel);
			return;
		case "w":
			WriteNullable(RenderCardPile(GetPile(PileType.Exhaust)), channel);
			return;
		case "state":
			ShowState(channel);
			return;
		case "room":
			ShowCurrentRoom(channel);
			return;
		case "actions":
			ShowActions(channel);
			return;
		case "rewards":
			ShowPendingRewards(channel);
			return;
		case "next":
		case "paths":
			ShowAvailablePaths(channel);
			return;
		case "start":
			EnterStartingPoint(channel);
			return;
		case "complete":
		case "leave":
			CompleteCurrentRoom(channel);
			return;
		case "act":
			ShowAct(channel);
			return;
		case "history":
			ShowHistory(channel);
			return;
		case "where":
			ShowLocation(channel);
			return;
		case "e":
			EndTurn(channel);
			return;
		default:
			WriteLine("Invalid input.", channel);
			return;
		}
	}

	private bool IsExitCommand(string input)
	{
		if (_exitCommands.Contains(input))
		{
			_interactiveExitRequested = true;
			return true;
		}
		return false;
	}

	private myCliChoice? PromptChoice(string prompt, IEnumerable<myCliChoice> choices, InteractionChannel channel)
	{
		_globalCommandBreak = false;
		myCliChoice? choice = _cli.PromptChoice(prompt, choices, channel, (string input) => TryHandleGlobalQuery(input, channel), IsExitCommand);
		if (choice == null && !_interactiveExitRequested && !_globalCommandBreak)
		{
			_interactiveExitRequested = true;
		}
		return choice;
	}

	private bool? TryHandleGlobalQuery(string command, InteractionChannel channel)
	{
		try
		{
			switch (command.Trim().ToLowerInvariant())
			{
			case "help":
				ShowHelp(channel);
				return true;
			case "m":
			case "map":
				WriteNullable(RenderMap(MapInstance, RunState, GetAvailableMapPoints()), channel);
				return true;
			case "c":
			case "deck":
				WriteNullable(RenderCardPile(GetDeckCards()), channel);
				return true;
			case "hp":
				WriteNullable(GetPlayerHpText(), channel);
				return true;
			case "g":
			case "gold":
				WriteNullable(GetPlayerGoldText(), channel);
				return true;
			case "a":
			case "draw":
				WriteNullable(RenderCardPile(GetPile(PileType.Draw)), channel);
				return true;
			case "q":
			case "discard":
				WriteNullable(RenderCardPile(GetPile(PileType.Discard)), channel);
				return true;
			case "w":
			case "exhaust":
				WriteNullable(RenderCardPile(GetPile(PileType.Exhaust)), channel);
				return true;
			case "state":
				ShowState(channel);
				return true;
			case "room":
				ShowCurrentRoom(channel);
				return true;
			case "actions":
				ShowCurrentPrompt(channel);
				return true;
			case "rewards":
				ShowPendingRewards(channel);
				return true;
			case "next":
			case "paths":
				ShowAvailablePaths(channel);
				return true;
			case "r":
			case "relic":
				ShowRelics(channel);
				return true;
			case "sl":
				ReloadSave(channel);
				_globalCommandBreak = true;
				return null;
			case "new":
				showImportant("Starting a new game.", channel);
				throw new NewRunRequestedException();
			case "act":
				ShowAct(channel);
				return true;
			case "history":
				ShowHistory(channel);
				return true;
			case "where":
				ShowLocation(channel);
				return true;
			case "combat":
			case "hand":
			case "targets":
			case "potions":
				ShowCombatState(channel);
				return true;
			default:
				return false;
			}
		}
		catch (NewRunRequestedException)
		{
			throw;
		}
		catch (Exception ex)
		{
			WriteLine($"Error: {ex.Message}", channel);
			return true;
		}
	}

	private IDisposable PushGlobalQueryHandler(InteractionChannel channel)
	{
		return Program.PushGlobalInputHandler((string input) => TryHandleGlobalQuery(input.Trim().TrimStart('\uFEFF'), channel) ?? false);
	}

	private void PromptForCurrentState(InteractionChannel channel)
	{
		if (PendingRewards != null && !PendingRewards.IsEmpty)
		{
			PromptRewardMenu(channel);
			return;
		}
		if (CombatSession != null && State == GameState.InCombat)
		{
			PromptCombatAction(channel);
			return;
		}
		if (EventBridge != null && CurrentRoom is EventRoom)
		{
			if (!EventBridge.IsFinished)
			{
				PromptEventOption(channel);
				return;
			}
			CompleteCurrentRoom(channel);
			return;
		}
		if (CurrentRoom is MerchantRoom merchantRoom)
		{
			PromptShop(merchantRoom, channel);
			return;
		}
		if (CurrentRoom is TreasureRoom && !_treasureOpened)
		{
			PromptTreasureRoom(channel);
			return;
		}
		if (CurrentRoom is RestSiteRoom && !_restActionTaken)
		{
			PromptRestSite(channel);
			return;
		}
		if (CurrentRoom != null && CurrentRoomState == RoomState.Active)
		{
			CompleteCurrentRoom(channel);
			return;
		}
		PromptMapSelection(channel);
	}

	private void ShowCurrentPrompt(InteractionChannel channel)
	{
		if (PendingRewards != null && !PendingRewards.IsEmpty)
		{
			ShowPendingRewards(channel);
			return;
		}
		if (CombatSession != null && State == GameState.InCombat)
		{
			ShowCombatState(channel);
			return;
		}
		if (EventBridge != null && CurrentRoom is EventRoom)
		{
			WriteLine(EventBridge.BuildSummary(), channel);
			return;
		}
		if (CurrentRoom is MerchantRoom merchantRoom)
		{
			ShowShop(merchantRoom, channel);
			return;
		}
		ShowAvailablePaths(channel);
	}

	private void PromptMapSelection(InteractionChannel channel)
	{
		IReadOnlyList<MapPoint> points = GetAvailableMapPoints();
		if (points.Count == 0)
		{
			ShowAvailablePaths(channel);
			_interactiveExitRequested = true;
			return;
		}
		List<myCliChoice> choices = new List<myCliChoice>();
		for (int i = 0; i < points.Count; i++)
		{
			MapPoint point = points[i];
			choices.Add(new myCliChoice
			{
				Key = (i + 1).ToString(),
				Index = i + 1,
				Text = $"{FormatMapPointForChoice(point)} ({point.coord.col},{point.coord.row})",
				Payload = point
			});
		}
		myCliChoice? choice = PromptChoice("Choose map node: ", choices, channel);
		if (choice?.Payload is MapPoint selectedPoint)
		{
			EnterMapCoord(selectedPoint.coord, channel);
		}
	}

	private void PromptEventOption(InteractionChannel channel)
	{
		myEventExecutionBridge bridge = RequireEventBridge();
		WriteLine(bridge.BuildSummary(), channel);
		IReadOnlyList<myEventOption> eventOptions = bridge.Event.CurrentOptions;
		List<myCliChoice> choices = new List<myCliChoice>();
		for (int i = 0; i < eventOptions.Count; i++)
		{
			myEventOption option = eventOptions[i];
			string text = SafeFormat(option.Title, option.TextKey);
			if (option.IsLocked)
			{
				text += " [locked]";
			}
			choices.Add(new myCliChoice
			{
				Key = (i + 1).ToString(),
				Index = i + 1,
				Text = text,
				Payload = i
			});
		}
		myCliChoice? choice = PromptChoice("Choose event option: ", choices, channel);
		if (choice?.Payload is not int optionIndex)
		{
			return;
		}
		if (eventOptions[optionIndex].IsLocked)
		{
			WriteLine("That event option is locked.", channel);
			return;
		}
		ExecuteEventChoice(optionIndex, channel);
		if (EventBridge != null && EventBridge.IsFinished && CombatSession == null && (PendingRewards == null || PendingRewards.IsEmpty))
		{
			CompleteCurrentRoom(channel);
		}
	}

	private void PromptTreasureRoom(InteractionChannel channel)
	{
		myCliChoice? choice = PromptChoice("Treasure room: ", new[]
		{
			new myCliChoice
			{
				Key = "1",
				Index = 1,
				Text = "open treasure"
			}
		}, channel);
		if (choice != null)
		{
			OpenTreasureRoom(channel);
			if (PendingRewards == null || PendingRewards.IsEmpty)
			{
				CompleteCurrentRoom(channel);
			}
		}
	}

	private void PromptRestSite(InteractionChannel channel)
	{
		RestSiteRoom restSite = (RestSiteRoom)CurrentRoom!;
		IReadOnlyList<RestSiteOption> options = restSite.Options;
		if (options.Count == 0)
		{
			CompleteCurrentRoom(channel);
			return;
		}
		List<myCliChoice> choices = new List<myCliChoice>();
		for (int i = 0; i < options.Count; i++)
		{
			RestSiteOption option = options[i];
			choices.Add(new myCliChoice
			{
				Key = (i + 1).ToString(),
				Index = i + 1,
				Text = $"{option.OptionId}{(option.IsEnabled ? "" : " (disabled)")}",
				Payload = i
			});
		}
		myCliChoice? choice = PromptChoice("Rest site: ", choices, channel);
		if (choice?.Payload is not int optionIndex)
		{
			return;
		}
		RestSiteOption selected = options[optionIndex];
		if (!selected.IsEnabled)
		{
			WriteLine("That option is currently disabled.", channel);
			return;
		}
		bool accepted = selected.OnSelect();
		if (accepted)
		{
			_restActionTaken = true;
			if (PendingRewards == null || PendingRewards.IsEmpty)
			{
				CompleteCurrentRoom(channel);
			}
		}
	}

	private void PromptCombatAction(InteractionChannel channel)
	{
		List<myCliChoice> choices = BuildCombatPromptChoices();
		myCliChoice? choice = PromptChoice("Combat input: ", choices, channel);
		if (choice?.Payload is not CombatPromptAction action)
		{
			return;
		}
		switch (action.Kind)
		{
		case CombatPromptKind.Card:
			PlayCombatCardFromPrompt(action.Index, channel);
			return;
		case CombatPromptKind.Potion:
			UseCombatPotionFromPrompt(action.Index, channel);
			return;
		case CombatPromptKind.EndTurn:
			EndTurn(channel);
			return;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private List<myCliChoice> BuildCombatPromptChoices()
	{
		EnsureCombatPlayPhase();
		Player player = RequireActivePlayer();
		IReadOnlyList<CardModel> hand = player.PlayerCombatState.Hand.Cards;
		List<myCliChoice> choices = new List<myCliChoice>();
		for (int i = 0; i < hand.Count; i++)
		{
			CardModel card = hand[i];
			choices.Add(new myCliChoice
			{
				Key = FormatCombatHandKey(i),
				Index = i + 1,
				Text = FormatCombatCardChoice(card),
				Payload = new CombatPromptAction
				{
					Kind = CombatPromptKind.Card,
					Index = i
				}
			});
		}
		int potionKey = GetFirstCombatPotionKey(hand.Count);
		for (int slotIndex = 0; slotIndex < player.PotionSlots.Count; slotIndex++)
		{
			PotionModel? potion = player.PotionSlots[slotIndex];
			if (potion == null)
			{
				continue;
			}
			choices.Add(new myCliChoice
			{
				Key = (potionKey++).ToString(),
				Index = choices.Count + 1,
				Text = $"potion {SafeFormat(potion.Title, potion.Id.Entry)}",
				Payload = new CombatPromptAction
				{
					Kind = CombatPromptKind.Potion,
					Index = slotIndex
				}
			});
		}
		choices.Add(new myCliChoice
		{
			Key = "e",
			Index = choices.Count + 1,
			Text = "end turn",
			Payload = new CombatPromptAction
			{
				Kind = CombatPromptKind.EndTurn,
				Index = -1
			}
		});
		return choices;
	}

	private void PlayCombatCardFromPrompt(int zeroBasedHandIndex, InteractionChannel channel)
	{
		EnsureCombatPlayPhase();
		Player player = RequireActivePlayer();
		IReadOnlyList<CardModel> hand = player.PlayerCombatState.Hand.Cards;
		if (zeroBasedHandIndex < 0 || zeroBasedHandIndex >= hand.Count)
		{
			throw new InvalidOperationException($"Hand index must be between 1 and {hand.Count}.");
		}
		CardModel card = hand[zeroBasedHandIndex];
		if (!player.PlayerCombatState.HasEnoughResourcesFor(card, out UnplayableReason reason))
		{
			WriteLine($"Card '{FormatCardName(card)}' cannot be played right now: {reason}.", channel);
			return;
		}
		Creature? target = PromptCombatTarget(card.TargetType, (Creature creature) => card.IsValidTarget(creature), $"Card '{FormatCardName(card)}'", channel);
		if (_interactiveExitRequested)
		{
			return;
		}
		if (!card.IsValidTarget(target))
		{
			WriteLine($"Target is not valid for card '{FormatCardName(card)}'.", channel);
			return;
		}
		EnqueueCombatCard(card, target, channel);
	}

	private void UseCombatPotionFromPrompt(int zeroBasedSlotIndex, InteractionChannel channel)
	{
		EnsureCombatPlayPhase();
		Player player = RequireActivePlayer();
		if (zeroBasedSlotIndex < 0 || zeroBasedSlotIndex >= player.PotionSlots.Count)
		{
			throw new InvalidOperationException($"Potion slot index must be between 1 and {player.PotionSlots.Count}.");
		}
		PotionModel potion = player.GetPotionAtSlotIndex(zeroBasedSlotIndex) ?? throw new InvalidOperationException($"Potion slot {zeroBasedSlotIndex + 1} is empty.");
		if (potion.Usage == PotionUsage.Automatic)
		{
			WriteLine($"Potion '{SafeFormat(potion.Title, potion.Id.Entry)}' is automatic and cannot be manually used.", channel);
			return;
		}
		Creature? target = PromptCombatTarget(potion.TargetType, (Creature creature) => IsValidPotionTarget(potion, creature), $"Potion '{SafeFormat(potion.Title, potion.Id.Entry)}'", channel);
		if (_interactiveExitRequested)
		{
			return;
		}
		EnqueueCombatPotion(potion, target, channel);
	}

	private Creature? PromptCombatTarget(TargetType targetType, Func<Creature, bool> predicate, string sourceLabel, InteractionChannel channel)
	{
		if (!targetType.IsSingleTarget() || targetType == TargetType.TargetedNoCreature)
		{
			return null;
		}
		List<Creature> validTargets = GetOrderedCombatTargets()
			.Where((Creature creature) => creature.IsAlive)
			.Where(predicate)
			.ToList();
		if (validTargets.Count == 0)
		{
			throw new InvalidOperationException($"{sourceLabel} has no valid targets.");
		}
		if (targetType == TargetType.Self)
		{
			return validTargets[0];
		}
		if (validTargets.Count == 1 && targetType != TargetType.AnyEnemy)
		{
			return validTargets[0];
		}
		List<myCliChoice> choices = validTargets.Select((Creature target, int index) => new myCliChoice
		{
			Key = (index + 1).ToString(),
			Index = index + 1,
			Text = BuildTargetSummary(target, RequireActivePlayer()),
			Payload = target
		}).ToList();
		myCliChoice? choice = PromptChoice($"{sourceLabel} target: ", choices, channel);
		return choice?.Payload as Creature;
	}

	private void PromptRewardMenu(InteractionChannel channel)
	{
		myPendingRewardState rewards = RequirePendingRewards();
		List<myCliChoice> choices = new List<myCliChoice>();
		for (int i = 0; i < rewards.Entries.Count; i++)
		{
			myPendingRewardEntry entry = rewards.Entries[i];
			choices.Add(new myCliChoice
			{
				Key = (i + 1).ToString(),
				Index = i + 1,
				Text = FormatRewardChoice(entry),
				Payload = new RewardPromptAction
				{
					Kind = RewardPromptKind.TakeReward,
					RewardIndex = i
				}
			});
		}
		choices.Add(new myCliChoice
		{
			Key = (choices.Count + 1).ToString(),
			Index = choices.Count + 1,
			Text = "skip",
			Payload = new RewardPromptAction
			{
				Kind = RewardPromptKind.SkipAll,
				RewardIndex = -1
			}
		});
		myCliChoice? choice = PromptChoice("Rewards: ", choices, channel);
		if (choice?.Payload is not RewardPromptAction action)
		{
			return;
		}
		if (action.Kind == RewardPromptKind.SkipAll)
		{
			SkipAllPendingRewardsAndLeave(channel);
			return;
		}
		myPendingRewardEntry entryToTake = rewards.GetEntry(action.RewardIndex);
		if (entryToTake.Kind == myPendingRewardKind.Card)
		{
			PromptCardReward(action.RewardIndex, entryToTake, channel);
			return;
		}
		TakeReward(action.RewardIndex, -1, channel);
		if (PendingRewards == null || PendingRewards.IsEmpty)
		{
			CompleteCurrentRoom(channel);
		}
	}

	private void PromptCardReward(int rewardIndex, myPendingRewardEntry entry, InteractionChannel channel)
	{
		List<myCliChoice> choices = new List<myCliChoice>();
		for (int i = 0; i < entry.Cards.Count; i++)
		{
			CardModel card = entry.Cards[i];
			choices.Add(new myCliChoice
			{
				Key = (i + 1).ToString(),
				Index = i + 1,
				Text = FormatCardName(card),
				Payload = i
			});
		}
		choices.Add(new myCliChoice
		{
			Key = (choices.Count + 1).ToString(),
			Index = choices.Count + 1,
			Text = "skip",
			Payload = -1
		});
		myCliChoice? choice = PromptChoice("Choose card reward: ", choices, channel);
		if (choice?.Payload is not int cardIndex)
		{
			return;
		}
		if (cardIndex < 0)
		{
			WriteLine("Skipped card choice; the card reward remains available.", channel);
			return;
		}
		TakeReward(rewardIndex, cardIndex, channel);
		if (PendingRewards == null || PendingRewards.IsEmpty)
		{
			CompleteCurrentRoom(channel);
		}
	}

	private void SkipAllPendingRewardsAndLeave(InteractionChannel channel)
	{
		int count = PendingRewards?.Entries.Count ?? 0;
		PendingRewards = null;
		WriteLine($"Skipped {count} remaining reward(s).", channel);
		if (CurrentRoom != null && CombatSession == null && (EventBridge == null || EventBridge.IsFinished))
		{
			CompleteCurrentRoom(channel);
		}
	}

	private void PromptShop(MerchantRoom room, InteractionChannel channel)
	{
		ShowShop(room, channel);
		List<MerchantEntry> entries = room.Inventory.AllEntries.Where((MerchantEntry entry) => entry.IsStocked).ToList();
		List<myCliChoice> choices = new List<myCliChoice>();
		for (int i = 0; i < entries.Count; i++)
		{
			MerchantEntry entry = entries[i];
			choices.Add(new myCliChoice
			{
				Key = FormatTwoDigitKey(i + 1),
				Aliases = new[] { (i + 1).ToString() },
				Index = i + 1,
				Text = FormatShopEntry(entry),
				Payload = new ShopPromptAction
				{
					Kind = ShopPromptKind.Purchase,
					Entry = entry
				}
			});
		}
		choices.Add(new myCliChoice
		{
			Key = FormatTwoDigitKey(choices.Count + 1),
			Aliases = new[] { (choices.Count + 1).ToString() },
			Index = choices.Count + 1,
			Text = "leave shop",
			Payload = new ShopPromptAction
			{
				Kind = ShopPromptKind.Leave
			}
		});
		myCliChoice? choice = PromptChoice("Shop: ", choices, channel);
		if (choice?.Payload is not ShopPromptAction action)
		{
			return;
		}
		if (action.Kind == ShopPromptKind.Leave)
		{
			CompleteCurrentRoom(channel);
			return;
		}
		if (action.Entry != null)
		{
			PurchaseShopEntry(room, action.Entry, channel);
		}
	}

	private void ShowShop(MerchantRoom room, InteractionChannel channel)
	{
		Player player = RequireActivePlayer();
		WriteLine($"Shop | Gold={player.Gold}", channel);
	}

	private void PurchaseShopEntry(MerchantRoom room, MerchantEntry entry, InteractionChannel channel)
	{
		if (!entry.IsStocked)
		{
			WriteLine("That shop entry is no longer stocked.", channel);
			return;
		}
		if (entry is MerchantCardRemovalEntry removalEntry)
		{
			PromptMerchantCardRemoval(removalEntry, channel);
			return;
		}
		bool success;
		using (PushGlobalQueryHandler(channel))
		{
			success = entry.OnTryPurchaseWrapper(room.Inventory);
		}
		if (!success)
		{
			WriteLine($"Purchase failed: {FormatShopEntry(entry)}.", channel);
			return;
		}
		WriteLine($"Purchased {FormatShopEntry(entry)}.", channel);
	}

	private void PromptMerchantCardRemoval(MerchantCardRemovalEntry removalEntry, InteractionChannel channel)
	{
		Player player = RequireActivePlayer();
		if (!removalEntry.EnoughGold)
		{
			WriteLine("Purchase failed: not enough gold.", channel);
			return;
		}
		List<CardModel> removableCards = player.Deck.Cards.Where((CardModel card) => card.IsRemovable).ToList();
		if (removableCards.Count == 0)
		{
			WriteLine("No removable cards are available.", channel);
			return;
		}
		List<myCliChoice> choices = removableCards.Select((CardModel card, int index) => new myCliChoice
		{
			Key = (index + 1).ToString(),
			Index = index + 1,
			Text = FormatCardName(card),
			Payload = card
		}).ToList();
		choices.Add(new myCliChoice
		{
			Key = (choices.Count + 1).ToString(),
			Index = choices.Count + 1,
			Text = "skip",
			Payload = null
		});
		myCliChoice? choice = PromptChoice("Remove card: ", choices, channel);
		if (choice?.Payload is not CardModel cardToRemove)
		{
			WriteLine("Skipped card removal.", channel);
			return;
		}
		int cost = removalEntry.Cost;
		CardPileCmd.RemoveFromDeck(cardToRemove);
		PlayerCmd.LoseGold(cost, player, GoldLossType.Spent).GetAwaiter().GetResult();
		player.ExtraFields.CardShopRemovalsUsed++;
		removalEntry.SetUsed();
		WriteLine($"Removed {FormatCardName(cardToRemove)} for {cost} gold.", channel);
	}

	private bool TryHandleTravelCommand(string command, InteractionChannel channel)
	{
		string[] parts = command.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return false;
		}
		string verb = parts[0];
		if (!verb.Equals("enter", StringComparison.OrdinalIgnoreCase) && !verb.Equals("go", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (parts.Length == 1)
		{
			ShowAvailablePaths(channel);
			return true;
		}
		if (parts.Length == 2)
		{
			if (parts[1].Equals("start", StringComparison.OrdinalIgnoreCase))
			{
				EnterStartingPoint(channel);
				return true;
			}
			if (int.TryParse(parts[1], out int index))
			{
				EnterAvailablePointByIndex(index, channel);
				return true;
			}
		}
		if (parts.Length >= 3 && int.TryParse(parts[1], out int col) && int.TryParse(parts[2], out int row))
		{
			EnterMapCoord(new MapCoord(col, row), channel);
			return true;
		}
		throw new InvalidOperationException("Use 'go <index>', 'go start', or 'enter <col> <row>'.");
	}

	private bool TryHandleChooseCommand(string command, InteractionChannel channel)
	{
		string[] parts = command.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return false;
		}
		string verb = parts[0];
		if (!verb.Equals("choose", StringComparison.OrdinalIgnoreCase) && !verb.Equals("select", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (CurrentRoom is not EventRoom)
		{
			throw new InvalidOperationException("There is no selectable event room right now.");
		}
		myEventExecutionBridge bridge = RequireEventBridge();
		if (bridge.IsSuspendedForCombat || State == GameState.InCombat)
		{
			throw new InvalidOperationException("The event is currently suspended for combat.");
		}
		if (parts.Length == 1)
		{
			WriteLine(bridge.BuildSummary(), channel);
			return true;
		}
		if (!int.TryParse(parts[1], out int optionIndex))
		{
			throw new InvalidOperationException("Use 'choose <index>'.");
		}
		ExecuteEventChoice(optionIndex - 1, channel);
		return true;
	}

	private bool TryHandleActionCommand(string command, InteractionChannel channel)
	{
		string[] parts = command.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return false;
		}
		string verb = parts[0];
		if (verb.Equals("do", StringComparison.OrdinalIgnoreCase))
		{
			if (parts.Length < 2 || !int.TryParse(parts[1], out int actionIndex))
			{
				throw new InvalidOperationException("Use 'do <index>'.");
			}
			ExecuteActionByIndex(actionIndex, channel);
			return true;
		}
		if (verb.Equals("take", StringComparison.OrdinalIgnoreCase))
		{
			if (parts.Length < 2 || !int.TryParse(parts[1], out int rewardIndex))
			{
				throw new InvalidOperationException("Use 'take <rewardIndex> [cardIndex]'.");
			}
			int cardIndex = -1;
			if (parts.Length >= 3)
			{
				if (!int.TryParse(parts[2], out int parsedCardIndex))
				{
					throw new InvalidOperationException("Use 'take <rewardIndex> [cardIndex]'.");
				}
				cardIndex = parsedCardIndex - 1;
			}
			TakeReward(rewardIndex - 1, cardIndex, channel);
			return true;
		}
		if (verb.Equals("skip", StringComparison.OrdinalIgnoreCase))
		{
			if (parts.Length < 2 || !int.TryParse(parts[1], out int rewardIndex))
			{
				throw new InvalidOperationException("Use 'skip <rewardIndex>'.");
			}
			SkipReward(rewardIndex - 1, channel);
			return true;
		}
		return false;
	}

	private bool TryHandleCombatCommand(string command, InteractionChannel channel)
	{
		string[] parts = command.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return false;
		}
		string verb = parts[0];
		if (verb.Equals("combat", StringComparison.OrdinalIgnoreCase))
		{
			ShowCombatState(channel);
			return true;
		}
		if (verb.Equals("play", StringComparison.OrdinalIgnoreCase))
		{
			if (parts.Length < 2 || !int.TryParse(parts[1], out int handIndex))
			{
				throw new InvalidOperationException("Use 'play <handIndex> [targetIndex]'.");
			}
			int? targetIndex = null;
			if (parts.Length >= 3)
			{
				if (!int.TryParse(parts[2], out int parsedTargetIndex))
				{
					throw new InvalidOperationException("Use 'play <handIndex> [targetIndex]'.");
				}
				targetIndex = parsedTargetIndex;
			}
			PlayCombatCard(handIndex, targetIndex, channel);
			return true;
		}
		if (verb.Equals("potion", StringComparison.OrdinalIgnoreCase) || verb.Equals("use", StringComparison.OrdinalIgnoreCase))
		{
			if (parts.Length < 2 || !int.TryParse(parts[1], out int slotIndex))
			{
				throw new InvalidOperationException("Use 'potion <slotIndex> [targetIndex]'.");
			}
			int? targetIndex2 = null;
			if (parts.Length >= 3)
			{
				if (!int.TryParse(parts[2], out int parsedTargetIndex2))
				{
					throw new InvalidOperationException("Use 'potion <slotIndex> [targetIndex]'.");
				}
				targetIndex2 = parsedTargetIndex2;
			}
			UseCombatPotion(slotIndex, targetIndex2, channel);
			return true;
		}
		if (verb.Equals("victory", StringComparison.OrdinalIgnoreCase) || verb.Equals("win", StringComparison.OrdinalIgnoreCase) || verb.Equals("defeat", StringComparison.OrdinalIgnoreCase) || verb.Equals("lose", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Manual victory/defeat commands have been removed. Use play, potion, and e so the real combat engine resolves the fight.");
		}
		if (verb.Equals("hand", StringComparison.OrdinalIgnoreCase) || verb.Equals("targets", StringComparison.OrdinalIgnoreCase) || verb.Equals("potions", StringComparison.OrdinalIgnoreCase))
		{
			ShowCombatState(channel);
			return true;
		}
		return false;
	}

	private void ShowHelp(InteractionChannel channel)
	{
		WriteLine("help: show help", channel);
		WriteLine("m / map: print map plus visited and reachable points", channel);
		WriteLine("state: print the global and room state machine status", channel);
		WriteLine("room: print the current room summary", channel);
		WriteLine("actions: list the actions currently available in the room/reward phase", channel);
		WriteLine("do <index>: execute one of the listed actions", channel);
		WriteLine("rewards: print pending rewards", channel);
		WriteLine("take <rewardIndex> [cardIndex]: claim a reward or choose a card option", channel);
		WriteLine("skip <rewardIndex>: skip a skippable reward", channel);
		WriteLine("next / paths: list currently reachable map points", channel);
		WriteLine("start: enter the starting map point", channel);
		WriteLine("go <index>: enter one of the currently reachable map points", channel);
		WriteLine("enter <col> <row>: enter a reachable map coord directly", channel);
		WriteLine("complete / leave: mark the current room complete and return to the map", channel);
		WriteLine("where: print current act, floor, coord, and room", channel);
		WriteLine("act: print current act summary", channel);
		WriteLine("history: print room history for the current act", channel);
		WriteLine("c / hp / g / a / q / w: print deck, HP, gold, and draw/discard/exhaust piles", channel);
		WriteLine("r / relic: print current relics", channel);
		WriteLine("sl: reload the save file", channel);
		WriteLine("new: abandon the current run state and start a new game from character selection", channel);
		WriteLine("choose <index>: execute the current event option", channel);
		WriteLine("combat: print the live combat summary with hand, targets, potions, and intents", channel);
		WriteLine("play <handIndex> [targetIndex]: play a card from hand through the combat engine", channel);
		WriteLine("potion <slotIndex> [targetIndex]: use a potion through the combat engine", channel);
		WriteLine("e / end: end the player turn and let monsters act", channel);
	}

	private void ShowState(InteractionChannel channel)
	{
		StringBuilder builder = new StringBuilder();
		builder.Append("GameState=").Append(State);
		builder.Append(" | RoomState=").Append(CurrentRoomState);
		builder.Append(" | Seed=").Append(string.IsNullOrWhiteSpace(Seed) ? "(none)" : Seed);
		if (RunState != null)
		{
			builder.Append(" | Act=").Append(RunState.CurrentActIndex + 1).Append('/').Append(RunState.Acts.Count);
			builder.Append(" | Floor=").Append(RunState.ActFloor);
			builder.Append(" | CurrentCoord=").Append(FormatCoord(RunState.CurrentMapCoord));
		}
		builder.Append(" | Room=").Append(CurrentRoom == null ? "(none)" : BuildRoomLabel(CurrentRoom));
		if (EventBridge != null)
		{
			builder.Append(" | EventFinished=").Append(EventBridge.IsFinished);
			builder.Append(" | EventSuspended=").Append(EventBridge.IsSuspendedForCombat);
		}
		if (CombatSession != null)
		{
			builder.Append(" | CombatState=").Append(CombatSession.State);
			builder.Append(" | CombatResult=").Append(CombatSession.Result);
			builder.Append(" | CombatRound=").Append(CombatSession.Room.CombatState.RoundNumber);
			builder.Append(" | CombatSide=").Append(CombatSession.Room.CombatState.CurrentSide);
			builder.Append(" | PlayPhase=").Append(CombatManager.Instance.IsPlayPhase);
		}
		if (PendingRewards != null && !PendingRewards.IsEmpty)
		{
			builder.Append(" | PendingRewards=").Append(PendingRewards.Entries.Count);
		}
		WriteLine(builder.ToString(), channel);
	}

	private void ShowCurrentRoom(InteractionChannel channel)
	{
		if (CurrentRoom == null)
		{
			if (PendingRewards != null && !PendingRewards.IsEmpty)
			{
				ShowPendingRewards(channel);
				ShowActions(channel);
				return;
			}
			WriteLine("No active room.", channel);
			return;
		}
		if (CombatSession != null && State == GameState.InCombat)
		{
			ShowCombatState(channel);
			return;
		}
		if (EventBridge != null && CurrentRoom is EventRoom)
		{
			WriteLine(EventBridge.BuildSummary(), channel);
			if (PendingRewards != null && !PendingRewards.IsEmpty)
			{
				WriteLine(PendingRewards.BuildSummary(), channel);
			}
			ShowActions(channel);
			return;
		}
		WriteLine(BuildRoomSummary(CurrentRoom), channel);
		if (PendingRewards != null && !PendingRewards.IsEmpty)
		{
			WriteLine(PendingRewards.BuildSummary(), channel);
		}
		ShowActions(channel);
	}

	private void ShowAvailablePaths(InteractionChannel channel)
	{
		IReadOnlyList<MapPoint> points = GetAvailableMapPoints();
		if (points.Count == 0)
		{
			if (CurrentRoom != null && CurrentRoomState == RoomState.Active)
			{
				WriteLine("Complete the current room before choosing another map point.", channel);
				return;
			}
			WriteLine("No map points are available right now.", channel);
			return;
		}
		WriteLine("Available map points:", channel);
		for (int i = 0; i < points.Count; i++)
		{
			MapPoint point = points[i];
			WriteLine($"{i + 1}: {FormatMapPointForChoice(point)} ({point.coord.col},{point.coord.row})", channel);
		}
	}

	private void ShowAct(InteractionChannel channel)
	{
		RunState runState = RequireRunState();
		WriteLine($"Act {runState.CurrentActIndex + 1}/{runState.Acts.Count}: {GetActName(runState.Act.SourceAct)}", channel);
	}

	private void ShowLocation(InteractionChannel channel)
	{
		RunState runState = RequireRunState();
		WriteLine($"Act={runState.CurrentActIndex + 1} | Floor={runState.ActFloor} | Coord={FormatCoord(runState.CurrentMapCoord)} | Room={(CurrentRoom == null ? "(none)" : BuildRoomLabel(CurrentRoom))}", channel);
	}

	private void ShowRelics(InteractionChannel channel)
	{
		Player? player = TryGetActivePlayer();
		if (player == null)
		{
			WriteLine("No active player.", channel);
			return;
		}
		if (player.Relics.Count == 0)
		{
			WriteLine("No relics.", channel);
			return;
		}
		for (int i = 0; i < player.Relics.Count; i++)
		{
			RelicModel relic = player.Relics[i];
			string name = relic.Title != null ? SafeFormat(relic.Title, relic.Id.Entry) : relic.Id.Entry;
			WriteLine($"  {i + 1}. {name} [{relic.Id}]", channel);
		}
	}

	private void ReloadSave(InteractionChannel channel)
	{
		if (!SaveManager.Instance.HasRunSave)
		{
			WriteLine("No save file found.", channel);
			return;
		}
		ReadSaveResult<SerializableRun> saveResult = SaveManager.Instance.LoadRunSave();
		if (!saveResult.Success || saveResult.SaveData == null)
		{
			WriteLine($"Failed to load save: {saveResult.Status}", channel);
			if (!string.IsNullOrWhiteSpace(saveResult.ErrorMessage))
			{
				WriteLine(saveResult.ErrorMessage, channel);
			}
			return;
		}
		bool replaceTreasure = _replaceTreasureWithElites;
		// 仅重载状态，不调用 ShowCurrentPrompt；由外层主循环重新分发
		LoadSavedRunSilent(saveResult.SaveData, replaceTreasure, channel);
	}

	private void ShowHistory(InteractionChannel channel)
	{
		RunState runState = RequireRunState();
		if (runState.CurrentActIndex >= runState.MapPointHistory.Count)
		{
			WriteLine("No room history has been recorded for the current act yet.", channel);
			return;
		}
		IReadOnlyList<MapPointHistoryEntry> entries = runState.MapPointHistory[runState.CurrentActIndex];
		if (entries.Count == 0)
		{
			WriteLine("No room history has been recorded for the current act yet.", channel);
			return;
		}
		WriteLine("Current act room history:", channel);
		for (int i = 0; i < entries.Count; i++)
		{
			MapPointHistoryEntry entry = entries[i];
			string rooms = entry.Rooms.Count == 0
				? "(none)"
				: string.Join(", ", entry.Rooms.Select(FormatRoomHistoryEntry));
			WriteLine($"{i + 1}. NodeType={entry.MapPointType} | Rooms={rooms}", channel);
		}
	}

	private void ShowActions(InteractionChannel channel)
	{
		if (CombatSession != null && State == GameState.InCombat)
		{
			WriteLine("Combat commands: combat | play <handIndex> [targetIndex] | potion <slotIndex> [targetIndex] | e", channel);
			return;
		}
		List<ActionChoice> actions = BuildCurrentActions().ToList();
		if (actions.Count == 0)
		{
			WriteLine("No immediate actions are available.", channel);
			return;
		}
		WriteLine("Available actions:", channel);
		for (int i = 0; i < actions.Count; i++)
		{
			WriteLine($"{i + 1}. {actions[i].Label}", channel);
		}
	}

	private void ShowPendingRewards(InteractionChannel channel)
	{
		if (PendingRewards == null || PendingRewards.IsEmpty)
		{
			WriteLine("No pending rewards.", channel);
			return;
		}
		WriteLine(PendingRewards.BuildSummary(), channel);
	}

	private void BeginEventRoom(EventRoom room, InteractionChannel channel)
	{
		EventBridge = new myEventExecutionBridge(room, RequireActivePlayer(), RequireRunState());
		using (PushGlobalQueryHandler(channel))
		{
			EventBridge.Begin();
		}
		State = GameState.InRoom;
		CurrentRoomState = RoomState.Active;
		WriteLine(EventBridge.BuildSummary(), channel);
		myEventCombatRequest? request = EventBridge.TakePendingCombatRequest();
		if (request != null)
		{
			BeginEventCombat(request, channel);
		}
	}

	private void BeginEventCombat(myEventCombatRequest request, InteractionChannel channel)
	{
		if (request == null)
		{
			throw new ArgumentNullException(nameof(request));
		}
		RunState runState = RequireRunState();
		myEventExecutionBridge bridge = RequireEventBridge();
		CombatRoom combatRoom = new CombatRoom(request.Encounter, runState);
		runState.PushRoom(combatRoom);
		AppendRoomHistoryEntry(combatRoom, runState);
		bridge.AppendCombatRoomToHistory(combatRoom);
		BeginCombatSession(myCombatSession.CreateForEventRequest(combatRoom, bridge, request), channel);
	}

	private void BeginCombatSession(myCombatSession session, InteractionChannel channel)
	{
		CombatSession = session ?? throw new ArgumentNullException(nameof(session));
		PendingRewards = null;
		CombatSession.Begin();
		State = GameState.InCombat;
		CurrentRoomState = RoomState.Active;
		EnsureActionExecutorReady();
		CombatSession.Room.Enter(RequireRunState(), isRestoringRoomStackBase: false);
		WaitForActionQueueToSettle();
		if (!FinalizeCombatIfEngineFinished(channel))
		{
			ShowCombatState(channel);
		}
	}

	private void FinishCombatSession(InteractionChannel channel)
	{
		RunState runState = RequireRunState();
		myCombatSession session = RequireCombatSession();
		CombatRoom combatRoom = session.Room;

		if (session.ParentEventBridge != null)
		{
			combatRoom.Exit(runState);
			AbstractRoom poppedRoom2 = runState.PopCurrentRoom();
			if (!ReferenceEquals(poppedRoom2, combatRoom))
			{
				throw new InvalidOperationException("Combat room stack became inconsistent while resolving combat.");
			}
			if (session.ShouldResumeParentEventAfterCombat)
			{
				myEventExecutionBridge bridge = session.ParentEventBridge;
				using (PushGlobalQueryHandler(channel))
				{
					bridge.ResumeAfterCombat(session);
				}
				CombatSession = null;
				session.MarkCompleted();
				State = GameState.InRoom;
				CurrentRoomState = RoomState.Active;
				WriteLine(bridge.BuildSummary(), channel);
				myEventCombatRequest? followUpRequest = bridge.TakePendingCombatRequest();
				if (followUpRequest != null)
				{
					BeginEventCombat(followUpRequest, channel);
					return;
				}
				if (bridge.IsFinished)
				{
					WriteLine("Event is finished.", channel);
				}
				return;
			}

			AbstractRoom baseRoom = runState.CurrentRoom ?? throw new InvalidOperationException("Expected the parent event room to remain on the room stack.");
			baseRoom.Exit(runState);
			session.ParentEventBridge.EnsureCleanup();
			runState.Act.MarkRoomVisited(RoomType.Event);
			runState.ClearRoomStack();
			EventBridge = null;
			CombatSession = null;
			session.MarkCompleted();
			CurrentRoomState = RoomState.None;

			if (session.Result == myCombatResult.Defeat)
			{
				State = GameState.RunEnded;
				showImportant("Run ended in event combat.", channel);
				return;
			}

			State = GameState.OnMap;
			WriteLine("Event combat resolved and the event room is complete.", channel);
			ShowAvailablePaths(channel);
			return;
		}

		if (session.Result == myCombatResult.Defeat)
		{
			combatRoom.Exit(runState);
			AbstractRoom poppedRoom = runState.PopCurrentRoom();
			if (!ReferenceEquals(poppedRoom, combatRoom))
			{
				throw new InvalidOperationException("Combat room stack became inconsistent while resolving combat.");
			}
			runState.Act.MarkRoomVisited(combatRoom.RoomType);
			runState.ClearRoomStack();
			CombatSession = null;
			session.MarkCompleted();
			CurrentRoomState = RoomState.None;
			State = GameState.RunEnded;
			showImportant("Run ended in combat.", channel);
			return;
		}

		PendingRewards = myHeadlessRewardRuntime.CreateCombatRewards(RequireActivePlayer(), runState, session);
		CombatSession = null;
		session.MarkCompleted();
		State = GameState.InRoom;
		CurrentRoomState = RoomState.Active;
		WriteLine($"Combat resolved for {BuildRoomLabel(combatRoom)}.", channel);
		if (PendingRewards != null && !PendingRewards.IsEmpty)
		{
			ShowPendingRewards(channel);
			return;
		}

		WriteLine("No rewards were generated.", channel);
		if (combatRoom.RoomType == RoomType.Boss && !GetAvailableMapPoints().Any())
		{
			if (runState.CurrentActIndex >= runState.Acts.Count - 1)
			{
				WriteLine("After completion the run will end.", channel);
			}
		}
	}

	private void ShowCombatState(InteractionChannel channel)
	{
		if (CombatSession == null || State != GameState.InCombat)
		{
			WriteLine("No active combat session.", channel);
			return;
		}
		Player player = RequireActivePlayer();
		CombatRoom room = RequireCurrentCombatRoom();
		CombatState combatState = room.CombatState;
		PlayerCombatState playerCombatState = player.PlayerCombatState ?? throw new InvalidOperationException("Player combat state is not initialized.");
		StringBuilder builder = new StringBuilder();
		builder.Append("Combat");
		builder.Append(" | Encounter=").Append(room.Encounter.Id.Entry);
		builder.Append(" | Type=").Append(room.RoomType);
		builder.Append(" | Round=").Append(combatState.RoundNumber);
		builder.Append(" | Side=").Append(combatState.CurrentSide);
		builder.Append(" | PlayPhase=").Append(CombatManager.Instance.IsPlayPhase);
		builder.Append(" | Ending=").Append(CombatManager.Instance.IsEnding);
		builder.AppendLine();
		builder.Append("Player: ").Append(player.Creature.Name);
		builder.Append(" | HP=").Append(player.Creature.CurrentHp).Append('/').Append(player.Creature.MaxHp);
		builder.Append(" | Block=").Append(player.Creature.Block);
		builder.Append(" | Energy=").Append(playerCombatState.Energy).Append('/').Append(playerCombatState.MaxEnergy);
		builder.Append(" | Stars=").Append(playerCombatState.Stars);
		AppendPowerSummary(builder, player.Creature);
		builder.AppendLine();
		builder.Append("Piles: Draw=").Append(playerCombatState.DrawPile.Cards.Count);
		builder.Append(" | Hand=").Append(playerCombatState.Hand.Cards.Count);
		builder.Append(" | Discard=").Append(playerCombatState.DiscardPile.Cards.Count);
		builder.Append(" | Exhaust=").Append(playerCombatState.ExhaustPile.Cards.Count);
		builder.Append(" | Play=").Append(playerCombatState.PlayPile.Cards.Count);
		builder.AppendLine();
		builder.Append("Potions:");
		if (player.PotionSlots.Count == 0)
		{
			builder.AppendLine();
			builder.Append("(none)");
		}
		else
		{
			int potionDisplayKey = GetFirstCombatPotionKey(playerCombatState.Hand.Cards.Count);
			for (int i = 0; i < player.PotionSlots.Count; i++)
			{
				PotionModel? potion = player.PotionSlots[i];
				builder.AppendLine();
				if (potion == null)
				{
					builder.Append(potionDisplayKey + i).Append(". (empty)");
					continue;
				}
				builder.Append(potionDisplayKey + i).Append(". ");
				builder.Append(SafeFormat(potion.Title, potion.Id.Entry));
				builder.Append(" [").Append(potion.Id.Entry).Append(']');
				builder.Append(" | Usage=").Append(potion.Usage);
				builder.Append(" | Target=").Append(potion.TargetType);
			}
		}
		builder.AppendLine();
		builder.Append("Hand:");
		if (playerCombatState.Hand.Cards.Count == 0)
		{
			builder.AppendLine();
			builder.Append("(empty)");
		}
		else
		{
			for (int j = 0; j < playerCombatState.Hand.Cards.Count; j++)
			{
				CardModel card = playerCombatState.Hand.Cards[j];
				bool hasEnoughResources = playerCombatState.HasEnoughResourcesFor(card, out UnplayableReason reason);
				bool passesPlayChecks = card.CanPlay(out UnplayableReason hookReason, out _);
				builder.AppendLine();
				builder.Append(FormatCombatHandKey(j)).Append(". ");
				builder.Append(FormatCardName(card)).Append(" [").Append(card.Id.Entry).Append(']');
				builder.Append(" | Cost=").Append(FormatCardCost(card));
				builder.Append(" | Target=").Append(card.TargetType);
				if (!hasEnoughResources)
				{
					builder.Append(" | Unplayable=").Append(reason);
				}
				else if (!passesPlayChecks)
				{
					builder.Append(" | Unplayable=").Append(hookReason);
				}
			}
		}
		IReadOnlyList<Creature> targets = GetOrderedCombatTargets();
		builder.AppendLine();
		builder.Append("Targets:");
		if (targets.Count == 0)
		{
			builder.AppendLine();
			builder.Append("(none)");
		}
		else
		{
			for (int k = 0; k < targets.Count; k++)
			{
				builder.AppendLine();
				builder.Append(k + 1).Append(". ").Append(BuildTargetSummary(targets[k], player));
			}
		}
		WriteLine(builder.ToString(), channel);
	}

	private void PrepareNewHeadlessRuntime(RunState runState)
	{
		ResetCoreRunManager();
		CoreRunManager.Instance.SetUpNewSinglePlayer(runState, shouldSave: true);
		CoreRunManager.Instance.CombatReplayWriter.IsEnabled = false;
		CoreRunManager.Instance.FinalizeStartingRelics();
		CoreRunManager.Instance.Launch();
		EnsureActionExecutorReady();
	}

	private void PrepareLoadedHeadlessRuntime(RunState runState, SerializableRun save)
	{
		ResetCoreRunManager();
		CoreRunManager.Instance.SetUpSavedSinglePlayer(runState, save);
		CoreRunManager.Instance.CombatReplayWriter.IsEnabled = false;
		CoreRunManager.Instance.Launch();
		EnsureActionExecutorReady();
	}

	private static void ResetCoreRunManager()
	{
		if (CoreRunManager.Instance.IsInProgress)
		{
			CoreRunManager.Instance.CleanUp(graceful: true);
		}
	}

	private static void ApplySavedMapsToLoad(myRunManager runManager, SerializableRun save)
	{
		runManager.SavedMapsToLoad = null;
		for (int i = 0; i < save.Acts.Count; i++)
		{
			SerializableActMap? savedMap = save.Acts[i].SavedMap;
			if (savedMap == null)
			{
				continue;
			}
			runManager.SavedMapsToLoad ??= new Dictionary<int, SerializableActMap>();
			runManager.SavedMapsToLoad[i] = savedMap;
		}
	}

	private void RestoreLoadedLocation(SerializableRun save, InteractionChannel channel)
	{
		RunState runState = RequireRunState();
		if (runState.VisitedMapCoords.Count == 0)
		{
			State = GameState.OnMap;
			CurrentRoomState = RoomState.None;
			return;
		}
		if (save.PreFinishedRoom != null)
		{
			RestoreLoadedPreFinishedRoom(save.PreFinishedRoom, channel);
			return;
		}
		int currentActHistoryCount = runState.MapPointHistory.ElementAtOrDefault(runState.CurrentActIndex)?.Count ?? 0;
		if (runState.VisitedMapCoords.Count > currentActHistoryCount)
		{
			RestoreLoadedPendingRoom(channel);
			return;
		}
		State = GameState.OnMap;
		CurrentRoomState = RoomState.None;
	}

	private void RestoreLoadedPendingRoom(InteractionChannel channel)
	{
		RunState runState = RequireRunState();
		ActMap map = RequireMap();
		MapCoord coord = runState.CurrentMapCoord ?? throw new InvalidOperationException("Loaded save is missing the latest visited map coord.");
		MapPoint point = map.GetPoint(coord) ?? throw new InvalidOperationException($"Map does not contain coord {coord} from the loaded save.");
		State = GameState.EnteringRoom;
		runState.ActFloor = coord.row + 1;
		RoomType roomType = RollRoomTypeFor(runState, point.PointType, myRunManager.BuildRoomTypeBlacklist(runState.CurrentMapPointHistoryEntry, runState.CurrentMapPoint?.Children ?? new HashSet<MapPoint>()));
		AbstractRoom room = CreateRoom(runState, roomType, point.PointType);
		runState.AppendToMapPointHistory(point.PointType, room.RoomType, room.ModelId);
		runState.ClearRoomStack();
		runState.PushRoom(room);
		CombatSession = null;
		EventBridge = null;
		PendingRewards = null;
		_treasureOpened = false;
		_restActionTaken = false;
		CurrentRoomState = RoomState.Created;
		if (room is not CombatRoom && room is not EventRoom)
		{
			room.Enter(runState, isRestoringRoomStackBase: false);
		}
		CurrentRoomState = RoomState.Entered;
		CurrentRoomState = RoomState.Active;
		showImportant($"Entered {BuildRoomLabel(room)} at ({coord.col}, {coord.row}).", channel);
		switch (room)
		{
		case EventRoom eventRoom:
			BeginEventRoom(eventRoom, channel);
			break;
		case CombatRoom combatRoom:
			BeginCombatSession(myCombatSession.CreateForRoom(combatRoom), channel);
			break;
		default:
			State = GameState.InRoom;
			WriteLine(BuildRoomSummary(room), channel);
			break;
		}
	}

	private void RestoreLoadedPreFinishedRoom(SerializableRoom serializableRoom, InteractionChannel channel)
	{
		RunState runState = RequireRunState();
		MapCoord coord = runState.CurrentMapCoord ?? throw new InvalidOperationException("Loaded save is missing the latest visited map coord.");
		AbstractRoom room = AbstractRoom.FromSerializable(serializableRoom, runState) ?? throw new InvalidOperationException("Loaded pre-finished room could not be restored.");
		runState.ClearRoomStack();
		runState.PushRoom(room);
		CombatSession = null;
		EventBridge = null;
		PendingRewards = null;
		_treasureOpened = room is TreasureRoom;
		_restActionTaken = room is RestSiteRoom;
		State = GameState.InRoom;
		CurrentRoomState = RoomState.Active;
		showImportant($"Restored {BuildRoomLabel(room)} at ({coord.col}, {coord.row}).", channel);
		if (room is CombatRoom combatRoom)
		{
			PendingRewards = myHeadlessRewardRuntime.CreateCombatRewards(RequireActivePlayer(), runState, combatRoom);
			if (PendingRewards != null && !PendingRewards.IsEmpty)
			{
				ShowPendingRewards(channel);
			}
			return;
		}
		WriteLine(BuildRoomSummary(room), channel);
	}

	private void UpdateCurrentMapPointHistoryPlayerStats()
	{
		RunState? runState = RunState;
		MapPointHistoryEntry? historyEntry = runState?.CurrentMapPointHistoryEntry;
		if (runState == null || historyEntry == null)
		{
			return;
		}
		foreach (Player player in runState.Players)
		{
			PlayerMapPointHistoryEntry? playerEntry = historyEntry.PlayerStats.FirstOrDefault((PlayerMapPointHistoryEntry entry) => entry.PlayerId == player.NetId);
			if (playerEntry == null)
			{
				continue;
			}
			playerEntry.CurrentGold = player.Gold;
			playerEntry.CurrentHp = player.Creature.CurrentHp;
			playerEntry.MaxHp = player.Creature.MaxHp;
		}
	}

	private static void EnsureActionExecutorReady()
	{
		if (!CoreRunManager.Instance.IsInProgress)
		{
			return;
		}
		if (CoreRunManager.Instance.ActionExecutor.IsPaused)
		{
			CoreRunManager.Instance.ActionExecutor.Unpause();
		}
	}

	private static void WaitForActionQueueToSettle()
	{
		if (!CoreRunManager.Instance.IsInProgress)
		{
			return;
		}
		EnsureActionExecutorReady();
		CoreRunManager.Instance.ActionExecutor.FinishedExecutingActions().GetAwaiter().GetResult();
	}

	private Player RequireActivePlayer()
	{
		RunState runState = RequireRunState();
		if (runState.Players.Count == 0)
		{
			throw new InvalidOperationException("No active player is attached to the current run.");
		}
		return runState.Players[0];
	}

	private CombatRoom RequireCurrentCombatRoom()
	{
		CombatRoom? combatRoom = CurrentRoom as CombatRoom ?? CombatSession?.Room;
		return combatRoom ?? throw new InvalidOperationException("There is no active combat room.");
	}

	private bool FinalizeCombatIfEngineFinished(InteractionChannel channel)
	{
		myCombatSession? session = CombatSession;
		if (session == null || State != GameState.InCombat)
		{
			return false;
		}
		if (CombatManager.Instance.IsInProgress)
		{
			return false;
		}
		if (!session.IsResolved)
		{
			if (session.Room.IsPreFinished)
			{
				session.ResolveVictory();
			}
			else
			{
				session.ResolveDefeat();
			}
		}
		FinishCombatSession(channel);
		return true;
	}

	private void EnsureCombatPlayPhase()
	{
		if (CombatSession == null || State != GameState.InCombat)
		{
			throw new InvalidOperationException("There is no active combat right now.");
		}
		if (!CombatManager.Instance.IsInProgress || !CombatManager.Instance.IsPlayPhase || RequireCurrentCombatRoom().CombatState.CurrentSide != CombatSide.Player)
		{
			throw new InvalidOperationException("Combat is not currently in the player play phase.");
		}
	}

	private void PlayCombatCard(int handIndex, int? targetIndex, InteractionChannel channel)
	{
		EnsureCombatPlayPhase();
		Player player = RequireActivePlayer();
		IReadOnlyList<CardModel> hand = player.PlayerCombatState.Hand.Cards;
		if (handIndex < 1 || handIndex > hand.Count)
		{
			throw new InvalidOperationException($"Hand index must be between 1 and {hand.Count}.");
		}
		CardModel card = hand[handIndex - 1];
		if (!player.PlayerCombatState.HasEnoughResourcesFor(card, out UnplayableReason reason))
		{
			throw new InvalidOperationException($"Card '{card.Title}' cannot be played right now: {reason}.");
		}
		Creature? target = ResolveCombatTarget(card.TargetType, (Creature creature) => card.IsValidTarget(creature), targetIndex, $"Card '{card.Title}'");
		if (!card.IsValidTarget(target))
		{
			throw new InvalidOperationException($"Target is not valid for card '{card.Title}'.");
		}
		EnqueueCombatCard(card, target, channel);
	}

	private void EnqueueCombatCard(CardModel card, Creature? target, InteractionChannel channel)
	{
		EnsureActionExecutorReady();
		using (PushGlobalQueryHandler(channel))
		{
			CoreRunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new PlayCardAction(card, target));
			WaitForActionQueueToSettle();
		}
		if (!FinalizeCombatIfEngineFinished(channel))
		{
			ShowCombatState(channel);
		}
	}

	private void UseCombatPotion(int slotIndex, int? targetIndex, InteractionChannel channel)
	{
		EnsureCombatPlayPhase();
		Player player = RequireActivePlayer();
		if (slotIndex < 1 || slotIndex > player.PotionSlots.Count)
		{
			throw new InvalidOperationException($"Potion slot index must be between 1 and {player.PotionSlots.Count}.");
		}
		PotionModel potion = player.GetPotionAtSlotIndex(slotIndex - 1) ?? throw new InvalidOperationException($"Potion slot {slotIndex} is empty.");
		if (potion.Usage == PotionUsage.Automatic)
		{
			throw new InvalidOperationException($"Potion '{SafeFormat(potion.Title, potion.Id.Entry)}' is automatic and cannot be manually used.");
		}
		Creature? target = ResolveCombatTarget(potion.TargetType, (Creature creature) => IsValidPotionTarget(potion, creature), targetIndex, $"Potion '{SafeFormat(potion.Title, potion.Id.Entry)}'");
		EnqueueCombatPotion(potion, target, channel);
	}

	private void EnqueueCombatPotion(PotionModel potion, Creature? target, InteractionChannel channel)
	{
		EnsureActionExecutorReady();
		using (PushGlobalQueryHandler(channel))
		{
			potion.EnqueueManualUse(target);
			WaitForActionQueueToSettle();
		}
		if (!FinalizeCombatIfEngineFinished(channel))
		{
			ShowCombatState(channel);
		}
	}

	private Creature? ResolveCombatTarget(TargetType targetType, Func<Creature, bool> predicate, int? oneBasedTargetIndex, string sourceLabel)
	{
		if (!targetType.IsSingleTarget() || targetType == TargetType.TargetedNoCreature)
		{
			return null;
		}
		List<Creature> validTargets = GetOrderedCombatTargets()
			.Where((Creature creature) => creature.IsAlive)
			.Where(predicate)
			.ToList();
		if (validTargets.Count == 0)
		{
			throw new InvalidOperationException($"{sourceLabel} has no valid targets.");
		}
		if (!oneBasedTargetIndex.HasValue)
		{
			if (validTargets.Count == 1)
			{
				return validTargets[0];
			}
			throw new InvalidOperationException($"{sourceLabel} requires a target. Use 'combat' to inspect the current target indexes.");
		}
		IReadOnlyList<Creature> displayedTargets = GetOrderedCombatTargets();
		if (oneBasedTargetIndex.Value < 1 || oneBasedTargetIndex.Value > displayedTargets.Count)
		{
			throw new InvalidOperationException($"Target index must be between 1 and {displayedTargets.Count} for {sourceLabel}.");
		}
		Creature selectedTarget = displayedTargets[oneBasedTargetIndex.Value - 1];
		if (!selectedTarget.IsAlive || !predicate(selectedTarget))
		{
			throw new InvalidOperationException($"Target {oneBasedTargetIndex.Value} is not valid for {sourceLabel}.");
		}
		return selectedTarget;
	}

	private IReadOnlyList<Creature> GetOrderedCombatTargets()
	{
		if (CombatSession == null)
		{
			return Array.Empty<Creature>();
		}
		CombatState combatState = RequireCurrentCombatRoom().CombatState;
		return combatState.PlayerCreatures
			.Concat(combatState.Enemies)
			.Where((Creature creature) => creature.IsAlive)
			.OrderBy((Creature creature) => creature.IsEnemy ? 1 : 0)
			.ThenBy((Creature creature) => creature.CombatId ?? uint.MaxValue)
			.ToList();
	}

	private static bool IsValidPotionTarget(PotionModel potion, Creature creature)
	{
		return potion.TargetType switch
		{
			TargetType.Self => ReferenceEquals(creature, potion.Owner.Creature),
			TargetType.AnyEnemy => creature.Side != potion.Owner.Creature.Side,
			TargetType.AnyPlayer => creature.Side == potion.Owner.Creature.Side,
			TargetType.AnyAlly => creature.Side == potion.Owner.Creature.Side && !ReferenceEquals(creature, potion.Owner.Creature),
			_ => true
		};
	}

	private static string FormatCardCost(CardModel card)
	{
		string energyCost = card.EnergyCost.CostsX ? "X" : card.GetEnergyCostToPay().ToString();
		int starCost = card.GetStarCostToPay();
		if (starCost < 0)
		{
			return $"{energyCost}E";
		}
		return $"{energyCost}E/{starCost}S";
	}

	private static string FormatCombatHandKey(int zeroBasedHandIndex)
	{
		return zeroBasedHandIndex == 9 ? "0" : (zeroBasedHandIndex + 1).ToString();
	}

	private static int GetFirstCombatPotionKey(int handCount)
	{
		return handCount >= 9 ? 10 : handCount + 1;
	}

	private static string FormatCombatCardChoice(CardModel card)
	{
		return $"{FormatCardName(card)} | cost {FormatCardCost(card)} | target {card.TargetType}";
	}

	private static string FormatCardName(CardModel card)
	{
		try
		{
			return card.Title;
		}
		catch
		{
			return card.Id.Entry;
		}
	}

	private static string FormatRewardChoice(myPendingRewardEntry entry)
	{
		return entry.Kind switch
		{
			myPendingRewardKind.Gold => $"gold x{entry.GoldAmount}",
			myPendingRewardKind.Potion => entry.Potion == null ? "potion" : $"potion {SafeFormat(entry.Potion.Title, entry.Potion.Id.Entry)}",
			myPendingRewardKind.Relic => entry.Relic == null ? "relic" : $"relic {SafeFormat(entry.Relic.Title, entry.Relic.Id.Entry)}",
			myPendingRewardKind.Card => "choose card",
			myPendingRewardKind.SpecialCard => entry.Cards.Count == 0 ? "special card" : $"special card {FormatCardName(entry.Cards[0])}",
			myPendingRewardKind.RemoveCard => "remove card",
			_ => entry.Label
		};
	}

	private static string FormatShopEntry(MerchantEntry entry)
	{
		string price = $"{entry.Cost}g";
		return entry switch
		{
			MerchantCardEntry cardEntry => cardEntry.CreationResult == null
				? $"card (sold) {price}"
				: $"card {FormatCardName(cardEntry.CreationResult.Card)} {price}",
			MerchantRelicEntry relicEntry => relicEntry.Model == null
				? $"relic (sold) {price}"
				: $"relic {SafeFormat(relicEntry.Model.Title, relicEntry.Model.Id.Entry)} {price}",
			MerchantPotionEntry potionEntry => potionEntry.Model == null
				? $"potion (sold) {price}"
				: $"potion {SafeFormat(potionEntry.Model.Title, potionEntry.Model.Id.Entry)} {price}",
			MerchantCardRemovalEntry => $"remove card {price}",
			_ => $"{entry.GetType().Name} {price}"
		};
	}

	private static string FormatTwoDigitKey(int index)
	{
		return index < 100 ? index.ToString("00") : index.ToString();
	}

	private static string FormatMapPointForChoice(MapPoint point)
	{
		return point.PointType switch
		{
			MapPointType.Ancient => "ancient",
			MapPointType.Monster => "monster",
			MapPointType.Unknown => "unknown",
			MapPointType.Shop => "shop",
			MapPointType.Treasure => "treasure",
			MapPointType.RestSite => "rest",
			MapPointType.Elite => "elite",
			MapPointType.Boss => "boss",
			_ => point.PointType.ToString().ToLowerInvariant()
		};
	}

	private static void AppendPowerSummary(StringBuilder builder, Creature creature)
	{
		if (creature.Powers.Count == 0)
		{
			return;
		}
		builder.Append(" | Powers=");
		builder.Append(string.Join(", ", creature.Powers.Select((PowerModel power) => $"{power.Id.Entry}({power.DisplayAmount})")));
	}

	private static string BuildTargetSummary(Creature creature, Player activePlayer)
	{
		StringBuilder builder = new StringBuilder();
		if (ReferenceEquals(creature, activePlayer.Creature))
		{
			builder.Append("Self");
		}
		else if (creature.IsEnemy)
		{
			builder.Append("Enemy");
		}
		else
		{
			builder.Append("Ally");
		}
		builder.Append(" ").Append(creature.Name);
		builder.Append(" | HP=").Append(creature.CurrentHp).Append('/').Append(creature.MaxHp);
		builder.Append(" | Block=").Append(creature.Block);
		if (creature.IsEnemy)
		{
			builder.Append(" | Intent=").Append(GetIntentSummary(creature));
		}
		if (creature.Powers.Count > 0)
		{
			builder.Append(" | Powers=").Append(string.Join(", ", creature.Powers.Select((PowerModel power) => $"{power.Id.Entry}({power.DisplayAmount})")));
		}
		return builder.ToString();
	}

	private static string GetIntentSummary(Creature creature)
	{
		if (!creature.IsEnemy || creature.Monster == null || creature.CombatState == null)
		{
			return "(none)";
		}
		List<string> labels = new List<string>();
		foreach (AbstractIntent intent in creature.Monster.NextMove.Intents)
		{
			string fallback = intent.IntentType.ToString();
			string label = SafeFormat(intent.GetIntentLabel(creature.CombatState.GetOpponentsOf(creature), creature), fallback);
			labels.Add(string.IsNullOrWhiteSpace(label) ? fallback : label);
		}
		return labels.Count == 0 ? "(none)" : string.Join(", ", labels);
	}

	private void EnterAvailablePointByIndex(int oneBasedIndex, InteractionChannel channel)
	{
		IReadOnlyList<MapPoint> points = GetAvailableMapPoints();
		if (oneBasedIndex < 1 || oneBasedIndex > points.Count)
		{
			throw new InvalidOperationException($"Reachable point index must be between 1 and {points.Count}.");
		}
		EnterMapCoord(points[oneBasedIndex - 1].coord, channel);
	}

	private void AdvanceToNextAct(InteractionChannel channel)
	{
		RunState runState = RequireRunState();
		if (runState.CurrentActIndex >= runState.Acts.Count - 1)
		{
			State = GameState.RunEnded;
			CurrentRoomState = RoomState.None;
			showImportant("Run complete.", channel);
			return;
		}
		State = GameState.TransitioningAct;
		runState.CurrentActIndex++;
		runState.Odds.UnknownMapPoint.ResetToBase();
		GenerateCurrentActMap();
		CurrentRoomState = RoomState.None;
		State = GameState.OnMap;
		showImportant($"Advanced to act {runState.CurrentActIndex + 1}: {GetActName(runState.Act.SourceAct)}.", channel);
	}

	private void GenerateCurrentActMap(bool replaceTreasureWithElites = false)
	{
		myRunManager runManager = RequireRunManager();
		runManager.GenerateMap(replaceTreasureWithElites);
		MapInstance = runManager.State.Map;
	}

	private AbstractRoom CreateRoom(RunState runState, RoomType roomType, MapPointType mapPointType)
	{
		return roomType switch
		{
			RoomType.Monster or RoomType.Elite or RoomType.Boss => new CombatRoom(runState.Act.PullNextEncounter(roomType).ToMutable(), runState),
			RoomType.Treasure => new TreasureRoom(runState.CurrentActIndex),
			RoomType.Shop => new MerchantRoom(),
			RoomType.Event => new EventRoom(mapPointType == MapPointType.Ancient ? runState.Act.PullAncient() : runState.Act.PullNextEvent(runState)),
			RoomType.RestSite => new RestSiteRoom(),
			RoomType.Map => new MapRoom(),
			_ => throw new InvalidOperationException($"Unexpected RoomType: {roomType}")
		};
	}

	private RoomType RollRoomTypeFor(RunState runState, MapPointType pointType, IEnumerable<RoomType> blacklist)
	{
		if (TryGetRoomTypeForTutorial(runState, pointType, out RoomType tutorialRoomType))
		{
			return tutorialRoomType;
		}
		return pointType switch
		{
			MapPointType.Unassigned => RoomType.Unassigned,
			MapPointType.Unknown => runState.Odds.UnknownMapPoint.Roll(blacklist ?? Array.Empty<RoomType>(), runState),
			MapPointType.Shop => RoomType.Shop,
			MapPointType.Treasure => RoomType.Treasure,
			MapPointType.RestSite => RoomType.RestSite,
			MapPointType.Monster => RoomType.Monster,
			MapPointType.Elite => RoomType.Elite,
			MapPointType.Boss => RoomType.Boss,
			MapPointType.Ancient => RoomType.Event,
			_ => throw new ArgumentOutOfRangeException(nameof(pointType), pointType, null)
		};
	}

	private static bool TryGetRoomTypeForTutorial(RunState runState, MapPointType pointType, out RoomType roomType)
	{
		roomType = RoomType.Unassigned;
		if (runState.PlayerCount > 1)
		{
			return false;
		}
		if (pointType != MapPointType.Unassigned)
		{
			return false;
		}
		if (runState.UnlockState.NumberOfRuns > 0)
		{
			return false;
		}
		if (runState.MapPointHistory.SelectMany((IReadOnlyList<MapPointHistoryEntry> list) => list).Any((MapPointHistoryEntry entry) => entry.MapPointType == MapPointType.Unassigned))
		{
			return false;
		}
		roomType = RoomType.Event;
		return true;
	}

	private void WriteLine(string message, InteractionChannel channel)
	{
		_cli.WriteLine(message, channel);
	}

	private void WritePrompt(string prompt, InteractionChannel channel)
	{
		if (_pipe != null)
		{
			_cli.ReadInput(prompt, channel);
			return;
		}
		_promptWriter(prompt);
	}

	private string? ReadInput(string prompt, InteractionChannel channel)
	{
		return _cli.ReadInput(prompt, channel);
	}

	private void WriteNullable(string? message, InteractionChannel channel)
	{
		WriteLine(message ?? "null", channel);
	}

	private IReadOnlyList<CardModel>? GetPile(PileType pileType)
	{
		Player? player = TryGetActivePlayer();
		if (player == null)
		{
			return null;
		}
		if (State == GameState.InCombat && CombatSession != null)
		{
			return pileType.GetPile(player).Cards.ToList();
		}
		return pileType switch
		{
			PileType.None => player.Deck.Cards.ToList(),
			_ => Array.Empty<CardModel>()
		};
	}

	private void EndTurn(InteractionChannel channel)
	{
		if (TryGetActivePlayer() == null)
		{
			WriteLine("null", channel);
			return;
		}
		if (State == GameState.InCombat)
		{
			EnsureCombatPlayPhase();
			using (PushGlobalQueryHandler(channel))
			{
				PlayerCmd.EndTurn(RequireActivePlayer(), canBackOut: false);
				WaitForActionQueueToSettle();
			}
			if (!FinalizeCombatIfEngineFinished(channel))
			{
				ShowCombatState(channel);
			}
			return;
		}
		WriteLine("End turn is only available during combat.", channel);
	}

	private void ExecuteEventChoice(int zeroBasedOptionIndex, InteractionChannel channel)
	{
		myEventExecutionBridge bridge = RequireEventBridge();
		myEventCombatRequest? request;
		using (PushGlobalQueryHandler(channel))
		{
			request = bridge.ChooseOption(zeroBasedOptionIndex);
		}
		WriteLine(bridge.BuildSummary(), channel);
		if (request != null)
		{
			BeginEventCombat(request, channel);
		}
		else if (bridge.IsFinished)
		{
			if (CurrentRoom is EventRoom eventRoom && eventRoom.CanonicalEvent is MegaCrit.Sts2.Core.Models.Events.AncientEventModel)
			{
				eventRoom.MarkPreFinished();
				SaveManager.Instance.SaveRun(eventRoom);
			}
			WriteLine("Event is finished.", channel);
		}
	}

	private void ExecuteActionByIndex(int oneBasedIndex, InteractionChannel channel)
	{
		List<ActionChoice> actions = BuildCurrentActions().ToList();
		if (oneBasedIndex < 1 || oneBasedIndex > actions.Count)
		{
			throw new InvalidOperationException($"Action index must be between 1 and {actions.Count}.");
		}
		ActionChoice action = actions[oneBasedIndex - 1];
		switch (action.Kind)
		{
		case ActionKind.EventOption:
			ExecuteEventChoice(action.PrimaryIndex, channel);
			return;
		case ActionKind.TreasureOpen:
			OpenTreasureRoom(channel);
			return;
		case ActionKind.RestHeal:
			UseRestSiteHeal(channel);
			return;
		case ActionKind.CompleteRoom:
			CompleteCurrentRoom(channel);
			return;
		case ActionKind.TakeReward:
			TakeReward(action.PrimaryIndex, action.SecondaryIndex, channel);
			return;
		case ActionKind.SkipReward:
			SkipReward(action.PrimaryIndex, channel);
			return;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private IEnumerable<ActionChoice> BuildCurrentActions()
	{
		if (PendingRewards != null && !PendingRewards.IsEmpty)
		{
			for (int rewardIndex = 0; rewardIndex < PendingRewards.Entries.Count; rewardIndex++)
			{
				myPendingRewardEntry reward = PendingRewards.Entries[rewardIndex];
				if (reward.Kind == myPendingRewardKind.Card)
				{
					for (int cardIndex = 0; cardIndex < reward.Cards.Count; cardIndex++)
					{
						yield return new ActionChoice
						{
							Kind = ActionKind.TakeReward,
							PrimaryIndex = rewardIndex,
							SecondaryIndex = cardIndex,
							Label = $"Take reward {rewardIndex + 1} card {cardIndex + 1} ({reward.Cards[cardIndex].Id})"
						};
					}
				}
				else
				{
					yield return new ActionChoice
					{
						Kind = ActionKind.TakeReward,
						PrimaryIndex = rewardIndex,
						Label = $"Take reward {rewardIndex + 1} ({reward.Label})"
					};
				}
				if (reward.CanSkip)
				{
					yield return new ActionChoice
					{
						Kind = ActionKind.SkipReward,
						PrimaryIndex = rewardIndex,
						Label = $"Skip reward {rewardIndex + 1}"
					};
				}
			}
			yield break;
		}

		if (EventBridge != null && CurrentRoom is EventRoom && !EventBridge.IsSuspendedForCombat)
		{
			IReadOnlyList<myEventOption> options = EventBridge.Event.CurrentOptions;
			for (int i = 0; i < options.Count; i++)
			{
				myEventOption option = options[i];
				yield return new ActionChoice
				{
					Kind = ActionKind.EventOption,
					PrimaryIndex = i,
					Label = $"Choose event option {i + 1}: {SafeFormat(option.Title, option.TextKey)}"
				};
			}
		}

		if (CurrentRoom is TreasureRoom && !_treasureOpened)
		{
			yield return new ActionChoice
			{
				Kind = ActionKind.TreasureOpen,
				Label = "Open the treasure chest"
			};
		}

		if (CurrentRoom is RestSiteRoom && !_restActionTaken)
		{
			yield return new ActionChoice
			{
				Kind = ActionKind.RestHeal,
				Label = "Rest and recover HP"
			};
		}

		if (CurrentRoom != null && CombatSession == null && (EventBridge == null || EventBridge.IsFinished))
		{
			yield return new ActionChoice
			{
				Kind = ActionKind.CompleteRoom,
				Label = $"Leave {BuildRoomLabel(CurrentRoom)}"
			};
		}
	}

	private void OpenTreasureRoom(InteractionChannel channel)
	{
		if (CurrentRoom is not TreasureRoom treasureRoom)
		{
			throw new InvalidOperationException("The current room is not a treasure room.");
		}
		if (_treasureOpened)
		{
			throw new InvalidOperationException("The treasure chest has already been opened.");
		}
		_treasureOpened = true;
		PendingRewards = myHeadlessRewardRuntime.CreateTreasureRewards(RequireActivePlayer(), RequireRunState(), treasureRoom);
		WriteLine("Opened the treasure chest.", channel);
		if (PendingRewards == null || PendingRewards.IsEmpty)
		{
			PendingRewards = null;
			WriteLine("The chest had no pending rewards.", channel);
			return;
		}
		ShowPendingRewards(channel);
	}

	private void UseRestSiteHeal(InteractionChannel channel)
	{
		if (CurrentRoom is not RestSiteRoom)
		{
			throw new InvalidOperationException("The current room is not a rest site.");
		}
		if (_restActionTaken)
		{
			throw new InvalidOperationException("A rest-site action has already been taken in this room.");
		}
		Player player = RequireActivePlayer();
		int oldHp = player.Creature.CurrentHp;
		PendingRewards = myHeadlessRewardRuntime.CreateRestSiteHealRewards(player, RequireRunState());
		_restActionTaken = true;
		int healed = Math.Max(0, player.Creature.CurrentHp - oldHp);
		WriteLine($"Recovered {healed} HP.", channel);
		if (PendingRewards == null || PendingRewards.IsEmpty)
		{
			PendingRewards = null;
			WriteLine("Rest complete.", channel);
			return;
		}
		ShowPendingRewards(channel);
	}

	private void TakeReward(int rewardIndex, int cardOptionIndex, InteractionChannel channel)
	{
		myPendingRewardState rewards = RequirePendingRewards();
		myPendingRewardEntry entry = rewards.GetEntry(rewardIndex);
		string result;
		using (PushGlobalQueryHandler(channel))
		{
			result = myHeadlessRewardRuntime.ApplyReward(RequireActivePlayer(), entry, cardOptionIndex);
		}
		rewards.RemoveAt(rewardIndex);
		WriteLine(result, channel);
		AfterRewardMutation(channel);
	}

	private void SkipReward(int rewardIndex, InteractionChannel channel)
	{
		myPendingRewardState rewards = RequirePendingRewards();
		myPendingRewardEntry entry = rewards.GetEntry(rewardIndex);
		if (!entry.CanSkip)
		{
			throw new InvalidOperationException("This reward cannot be skipped.");
		}
		rewards.RemoveAt(rewardIndex);
		WriteLine($"Skipped reward {rewardIndex + 1}.", channel);
		AfterRewardMutation(channel);
	}

	private void AfterRewardMutation(InteractionChannel channel)
	{
		if (PendingRewards == null || PendingRewards.IsEmpty)
		{
			PendingRewards = null;
			WriteLine("All pending rewards are resolved.", channel);
			return;
		}
		ShowPendingRewards(channel);
	}

	private Player? TryGetActivePlayer()
	{
		RunState? runState = RunState;
		if (runState == null || runState.Players.Count == 0)
		{
			return null;
		}
		return runState.Players[0];
	}

	private IReadOnlyList<CardModel>? GetDeckCards()
	{
		return TryGetActivePlayer()?.Deck.Cards.ToList();
	}

	private string? GetPlayerHpText()
	{
		Player? player = TryGetActivePlayer();
		return player == null ? null : $"{player.Creature.CurrentHp}/{player.Creature.MaxHp}";
	}

	private string? GetPlayerGoldText()
	{
		return TryGetActivePlayer()?.Gold.ToString();
	}

	private RunState RequireRunState()
	{
		return RunState ?? throw new InvalidOperationException("RunState is not initialized yet. Generate a map first.");
	}

	private myEventExecutionBridge RequireEventBridge()
	{
		return EventBridge ?? throw new InvalidOperationException("Event bridge is not active.");
	}

	private myCombatSession RequireCombatSession()
	{
		return CombatSession ?? throw new InvalidOperationException("Combat session is not active.");
	}

	private myPendingRewardState RequirePendingRewards()
	{
		return PendingRewards ?? throw new InvalidOperationException("There are no pending rewards right now.");
	}

	private myRunManager RequireRunManager()
	{
		return RunManager ?? throw new InvalidOperationException("RunManager is not initialized yet.");
	}

	private ActMap RequireMap()
	{
		return MapInstance ?? throw new InvalidOperationException("Map has not been generated yet.");
	}

	private static string BuildCharacterOption(CharacterModel character)
	{
		return $"{GetCharacterName(character)} (HP {character.StartingHp}, Gold {character.StartingGold})";
	}

	private static string BuildCharacterSummary(CharacterModel character)
	{
		string startingDeck = string.Join(", ", character.StartingDeck.Select((CardModel card) => card.Id));
		string startingRelics = string.Join(", ", character.StartingRelics.Select((RelicModel relic) => relic.Id));
		string startingPotions = string.Join(", ", character.StartingPotions.Select((PotionModel potion) => potion.Id));
		if (startingPotions.Length == 0)
		{
			startingPotions = "(none)";
		}
		return $"Character summary: {GetCharacterName(character)} | Starting deck: {startingDeck} | Starting relics: {startingRelics} | Starting potions: {startingPotions}";
	}

	private static string GetCharacterName(CharacterModel character)
	{
		try
		{
			return character.Title.GetFormattedText();
		}
		catch
		{
			return character.Id.Entry;
		}
	}

	private static string GetActName(ActModel act)
	{
		try
		{
			return act.Title.GetFormattedText();
		}
		catch
		{
			return act.Id.Entry;
		}
	}

	private static string? RenderMap(ActMap? map, RunState? runState, IReadOnlyList<MapPoint> availablePoints)
	{
		if (map == null)
		{
			return null;
		}
		StringBuilder builder = new StringBuilder();
		builder.AppendLine("Map legend: A=Start M=Monster ?=Unknown $=Shop T=Treasure R=Rest E=Elite B=Boss");
		builder.AppendLine();

		int colCount = map.GetColumnCount();
		int lastRow = map.GetRowCount();
		if (map.SecondBossMapPoint != null)
		{
			lastRow++;
		}

		// Print from top (Boss) to bottom (Starting point)
		for (int row = lastRow; row >= 0; row--)
		{
			// Room row
			for (int col = 0; col < colCount; col++)
			{
				MapPoint? point = GetPointAt(map, col, row);
				if (point != null && runState != null && runState.CurrentMapCoord.HasValue
					&& runState.CurrentMapCoord.Value.col == col && runState.CurrentMapCoord.Value.row == row)
				{
					builder.Append('*');
				}
				else
				{
					builder.Append(GetPointSymbol(point));
				}
				if (col < colCount - 1)
				{
					builder.Append("   ");
				}
			}
			builder.AppendLine();

			// Path row (connections from this row down to row-1)
			if (row > 0)
			{
				char[] pathRow = new char[colCount * 4 - 1];
				for (int i = 0; i < pathRow.Length; i++)
				{
					pathRow[i] = ' ';
				}

				for (int col = 0; col < colCount; col++)
				{
					MapPoint? point = GetPointAt(map, col, row);
					if (point == null)
					{
						continue;
					}
					foreach (MapPoint parent in point.parents)
					{
						int parentCol = parent.coord.col;
						int colPos = col * 4;
						if (parentCol == col)
						{
							pathRow[colPos] = '|';
						}
						else if (parentCol < col)
						{
							pathRow[colPos-1] = '/';
						}
						else
						{
							pathRow[colPos+1] = '\\';
						}
					}
				}
				builder.AppendLine(new string(pathRow));
			}
		}

		if (runState != null)
		{
			builder.AppendLine();
			builder.AppendLine($"Visited: {FormatCoordList(runState.VisitedMapCoords)}");
			builder.AppendLine($"Current: {FormatCoord(runState.CurrentMapCoord)}");
		}
		builder.Append($"Reachable: {FormatCoordList(availablePoints.Select((MapPoint point) => point.coord))}");
		return builder.ToString().TrimEnd();
	}

	private static MapPoint? GetPointAt(ActMap map, int col, int row)
	{
		if (map.StartingMapPoint.coord.col == col && map.StartingMapPoint.coord.row == row)
		{
			return map.StartingMapPoint;
		}
		if (map.BossMapPoint.coord.col == col && map.BossMapPoint.coord.row == row)
		{
			return map.BossMapPoint;
		}
		if (map.SecondBossMapPoint != null && map.SecondBossMapPoint.coord.col == col && map.SecondBossMapPoint.coord.row == row)
		{
			return map.SecondBossMapPoint;
		}
		if (row < 0 || row >= map.GetRowCount())
		{
			return null;
		}
		return map.GetPoint(new MapCoord(col, row));
	}

	private static string GetPointSymbol(MapPoint? point)
	{
		if (point == null)
		{
			return ".";
		}
		return point.PointType switch
		{
			MapPointType.Ancient => "A",
			MapPointType.Monster => "M",
			MapPointType.Unknown => "?",
			MapPointType.Shop => "$",
			MapPointType.Treasure => "T",
			MapPointType.RestSite => "R",
			MapPointType.Elite => "E",
			MapPointType.Boss => "B",
			_ => "."
		};
	}

	private static string? RenderCardPile(IEnumerable<CardModel>? pile)
	{
		if (pile == null)
		{
			return null;
		}
		List<CardModel> cards = pile.ToList();
		if (cards.Count == 0)
		{
			return "[]";
		}
		return $"[{string.Join(", ", cards.Select(FormatCardName))}]";
	}

	private static string BuildRoomLabel(AbstractRoom room)
	{
		return room switch
		{
			CombatRoom combatRoom => $"{room.RoomType}:{combatRoom.Encounter.Id.Entry}",
			EventRoom eventRoom => $"Event:{eventRoom.CanonicalEvent.Id}",
			_ => room.RoomType.ToString()
		};
	}

	private static string BuildRoomSummary(AbstractRoom room)
	{
		StringBuilder builder = new StringBuilder();
		switch (room)
		{
		case CombatRoom combatRoom:
			builder.Append("Combat room");
			builder.Append(" | Encounter=").Append(combatRoom.Encounter.Id.Entry);
			builder.Append(" | Type=").Append(combatRoom.RoomType);
			builder.Append(" | Status=managed by myCombatSession");
			break;
		case EventRoom eventRoom:
			builder.Append("Event room");
			builder.Append(" | Event=").Append(eventRoom.CanonicalEvent.Id);
			builder.AppendLine();
			builder.Append("Title: ").Append(SafeFormat(eventRoom.CanonicalEvent.Title, eventRoom.CanonicalEvent.Id));
			builder.AppendLine();
			builder.Append("Description: ").Append(SafeFormat(eventRoom.CanonicalEvent.InitialDescription, "(unavailable)"));
			IReadOnlyList<string> previewOptions = GetEventPreviewOptions(eventRoom);
			if (previewOptions.Count > 0)
			{
				builder.AppendLine();
				builder.Append("Preview options: ").Append(string.Join(" | ", previewOptions));
			}
			builder.AppendLine();
			builder.Append("Status=managed by myEventExecutionBridge once the room is entered");
			break;
		case TreasureRoom treasureRoom:
			builder.Append("Treasure room");
			builder.Append(" | Act=").Append(treasureRoom.ActIndex + 1);
			builder.Append(" | Status=managed by myGame");
			break;
		case MerchantRoom:
			builder.Append("Merchant room | Status=managed by myGame");
			break;
		case RestSiteRoom:
			builder.Append("Rest site room | Status=managed by myGame");
			break;
		case MapRoom:
			builder.Append("Map room");
			break;
		default:
			builder.Append(room.RoomType);
			break;
		}
		return builder.ToString();
	}

	private static IReadOnlyList<string> GetEventPreviewOptions(EventRoom room)
	{
		try
		{
			return room.CanonicalEvent.GameInfoOptions
				.Select((LocString option) => SafeFormat(option, option.LocEntryKey))
				.Where((string option) => !string.IsNullOrWhiteSpace(option))
				.ToList();
		}
		catch
		{
			return Array.Empty<string>();
		}
	}

	private static string DescribeMapPoint(MapPoint point)
	{
		return $"{point.PointType} | children={point.Children.Count}";
	}

	private static string FormatRoomHistoryEntry(MapPointRoomHistoryEntry roomEntry)
	{
		string modelId = roomEntry.ModelId?.Entry ?? "(none)";
		if (roomEntry.MonsterIds.Count == 0 && roomEntry.TurnsTaken == 0)
		{
			return $"{roomEntry.RoomType}:{modelId}";
		}
		return $"{roomEntry.RoomType}:{modelId} turns={roomEntry.TurnsTaken}";
	}

	private static string SafeFormat(LocString? locString, string fallback)
	{
		if (locString == null)
		{
			return fallback;
		}
		try
		{
			return locString.GetFormattedText();
		}
		catch
		{
			return fallback;
		}
	}

	private static void AppendRoomHistoryEntry(AbstractRoom room, RunState runState)
	{
		MapPointHistoryEntry? currentEntry = runState.CurrentMapPointHistoryEntry;
		if (currentEntry == null)
		{
			return;
		}
		currentEntry.Rooms.Add(new MapPointRoomHistoryEntry
		{
			RoomType = room.RoomType,
			ModelId = room.ModelId
		});
	}

	private static string FormatCoord(MapCoord? coord)
	{
		return coord.HasValue ? $"({coord.Value.col}, {coord.Value.row})" : "(none)";
	}

	private static string FormatCoordList(IEnumerable<MapCoord> coords)
	{
		List<MapCoord> resolved = coords?.ToList() ?? new List<MapCoord>();
		if (resolved.Count == 0)
		{
			return "[]";
		}
		return $"[{string.Join(", ", resolved.Select((MapCoord coord) => $"({coord.col}, {coord.row})"))}]";
	}
}
