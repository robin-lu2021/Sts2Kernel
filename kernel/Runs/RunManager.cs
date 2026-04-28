using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Runs.Metrics;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.MapDrawing;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Runs;

public class RunManager
{
	private long _startTime;

	private long _prevRunTime;

	private long _sessionStartTime;

	private bool _runHistoryWasUploaded;

	public Action? debugAfterCombatRewardsOverride;

	public static RunManager Instance { get; } = new RunManager();

	public AscensionManager AscensionManager { get; private set; } = null!;

	public bool ShouldSave { get; private set; }

	public DateTimeOffset? DailyTime { get; private set; }

	public bool IsInProgress => State != null;

	public bool IsCleaningUp { get; private set; }

	public bool ForceDiscoveryOrderModifications { get; set; }

	public bool IsGameOver
	{
		get
		{
			if (IsInProgress)
			{
				return State.IsGameOver;
			}
			return false;
		}
	}

	public bool IsAbandoned { get; private set; }

	public RunHistory? History { get; set; }

	public INetGameService NetService { get; private set; } = null!;

	public ChecksumTracker ChecksumTracker { get; private set; } = null!;

	public RunLocationTargetedMessageBuffer RunLocationTargetedBuffer { get; private set; } = null!;

	public CombatReplayWriter CombatReplayWriter { get; private set; } = null!;

	public CombatStateSynchronizer CombatStateSynchronizer { get; private set; } = null!;

	public MapSelectionSynchronizer MapSelectionSynchronizer { get; private set; } = null!;

	public ActChangeSynchronizer ActChangeSynchronizer { get; private set; } = null!;

	public PlayerChoiceSynchronizer PlayerChoiceSynchronizer { get; private set; } = null!;

	public EventSynchronizer EventSynchronizer { get; private set; } = null!;

	public RewardSynchronizer RewardSynchronizer { get; private set; } = null!;

	public RestSiteSynchronizer RestSiteSynchronizer { get; private set; } = null!;

	public OneOffSynchronizer OneOffSynchronizer { get; private set; } = null!;

	public TreasureRoomRelicSynchronizer TreasureRoomRelicSynchronizer { get; private set; } = null!;

	public FlavorSynchronizer FlavorSynchronizer { get; private set; } = null!;

	public PeerInputSynchronizer InputSynchronizer { get; private set; } = null!;

	public HoveredModelTracker HoveredModelTracker { get; private set; } = null!;

	public ActionQueueSet ActionQueueSet { get; private set; } = null!;

	public ActionExecutor ActionExecutor { get; private set; } = null!;

	public ActionQueueSynchronizer ActionQueueSynchronizer { get; private set; } = null!;

	public long WinTime { get; set; }

	public long RunTime
	{
		get
		{
			if (WinTime > 0)
			{
				return WinTime;
			}
			return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _sessionStartTime + _prevRunTime;
		}
	}

	public bool IsSinglePlayerOrFakeMultiplayer
	{
		get
		{
			if (IsInProgress)
			{
				return NetService.Type == NetGameType.Singleplayer;
			}
			return false;
		}
	}

	public SerializableMapDrawings? MapDrawingsToLoad { get; set; }

	public Dictionary<int, SerializableActMap>? SavedMapsToLoad { get; set; }

	private RunState? State { get; set; }

	public event Action<RunState>? RunStarted;

	public event Action? RoomEntered;

	public event Action? RoomExited;

	public event Action? ActEntered;

	private RunManager()
	{
	}

	public void SetUpNewSinglePlayer(RunState state, bool shouldSave, DateTimeOffset? dailyTime = null)
	{
		if (State != null)
		{
			throw new InvalidOperationException("State is already set.");
		}
		State = state;
		INetGameService netService = new NetSingleplayerGameService();
		InitializeShared(netService, new PeerInputSynchronizer(netService), shouldSave, dailyTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 0L, 0L);
		InitializeCombatStateSynchronizer(state);
		InitializeNewRun();
		GenerateRooms();
	}

	public void SetUpSavedSinglePlayer(RunState state, SerializableRun save)
	{
		if (State != null)
		{
			throw new InvalidOperationException("State is already set.");
		}
		State = state;
		INetGameService netService = new NetSingleplayerGameService();
		InitializeShared(netService, new PeerInputSynchronizer(netService), shouldSave: true, save.DailyTime, save.StartTime, save.RunTime, save.WinTime);
		InitializeCombatStateSynchronizer(state);
		InitializeSavedRun(save);
	}

	public void SetUpReplay(RunState state, CombatReplay replay)
	{
		if (State != null)
		{
			throw new InvalidOperationException("State is already set.");
		}
		State = state;
		SerializableRun serializableRun = replay.serializableRun;
		ulong netId = serializableRun.Players[0].NetId;
		NetReplayGameService netService = new NetReplayGameService(netId);
		InitializeShared(netService, new PeerInputSynchronizer(netService), shouldSave: true, serializableRun.DailyTime, serializableRun.StartTime, serializableRun.RunTime, serializableRun.WinTime);
		InitializeCombatStateSynchronizer(state);
		InitializeSavedRun(serializableRun);
	}

	public void SetUpTest(RunState state, INetGameService gameService, bool disableCombatStateSync = true, bool shouldSave = false)
	{
		if (State != null)
		{
			throw new InvalidOperationException("State is already set.");
		}
		State = state;
		InitializeShared(gameService, new PeerInputSynchronizer(gameService), shouldSave, null, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 0L, 0L);
		InitializeCombatStateSynchronizer(state);
		CombatStateSynchronizer.IsDisabled = disableCombatStateSync;
		InitializeNewRun();
	}

	private void InitializeShared(INetGameService netService, PeerInputSynchronizer inputSynchronizer, bool shouldSave, DateTimeOffset? dailyTime, long startTime, long runTime, long winTime)
	{
		if (State == null)
		{
			throw new InvalidOperationException("State is not set.");
		}
		NetService = netService;
		ulong netId = NetService.NetId;
		ChecksumTracker = new ChecksumTracker(NetService, State);
		ChecksumTracker.IsEnabled = !TestMode.IsOn && NetService.Type.IsMultiplayer();
		RunLocationTargetedBuffer = new RunLocationTargetedMessageBuffer(NetService);
		FlavorSynchronizer = new FlavorSynchronizer(NetService, State, netId);
		ActionQueueSet = new ActionQueueSet(State.Players);
		ActionExecutor = new ActionExecutor(ActionQueueSet);
		ActionQueueSynchronizer = new ActionQueueSynchronizer(State, ActionQueueSet, RunLocationTargetedBuffer, NetService);
		PlayerChoiceSynchronizer = new PlayerChoiceSynchronizer(NetService, State);
		MapSelectionSynchronizer = new MapSelectionSynchronizer(NetService, ActionQueueSynchronizer, State);
		ActChangeSynchronizer = new ActChangeSynchronizer(State);
		EventSynchronizer = new EventSynchronizer(RunLocationTargetedBuffer, NetService, State, netId, State.Rng.Seed);
		RewardSynchronizer = new RewardSynchronizer(RunLocationTargetedBuffer, NetService, State, netId);
		RestSiteSynchronizer = new RestSiteSynchronizer(RunLocationTargetedBuffer, NetService, State, netId);
		OneOffSynchronizer = new OneOffSynchronizer(RunLocationTargetedBuffer, NetService, State, netId);
		TreasureRoomRelicSynchronizer = new TreasureRoomRelicSynchronizer(State, netId, ActionQueueSynchronizer, State.SharedRelicGrabBag, State.Rng.TreasureRoomRelics);
		CombatReplayWriter = new CombatReplayWriter(PlayerChoiceSynchronizer, ActionQueueSet, ActionQueueSynchronizer, ChecksumTracker);
		CombatReplayWriter.IsEnabled = !TestMode.IsOn;
		ActionExecutor.JustBeforeActionFinishedExecuting += SendPostActionChecksum;
		ChecksumTracker.StateDiverged += StateDiverged;
		ActionExecutor.Pause();
		IsAbandoned = false;
		AscensionManager = new AscensionManager(State.AscensionLevel);
		ShouldSave = shouldSave;
		DailyTime = dailyTime;
		_startTime = startTime;
		_prevRunTime = runTime;
		WinTime = winTime;
		_sessionStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		InputSynchronizer = inputSynchronizer;
		HoveredModelTracker = new HoveredModelTracker(InputSynchronizer, State);
	}

	public void InitializeRunLobby(INetGameService netService, RunState state)
	{
		InitializeCombatStateSynchronizer(state);
	}

	public void InitializeCombatStateSynchronizer(RunState state)
	{
		CombatStateSynchronizer = new CombatStateSynchronizer(NetService, state);
	}

	private void InitializeNewRun()
	{
		State.SharedRelicGrabBag.Populate(ModelDb.RelicPool<SharedRelicPool>().GetUnlockedRelics(State.UnlockState), State.Rng.UpFront);
		foreach (Player player in State.Players)
		{
			player.PopulateRelicGrabBagIfNecessary(State.Rng.UpFront);
		}
		SetStartedWithNeowFlag();
		foreach (ModifierModel modifier in State.Modifiers)
		{
			modifier.OnRunCreated(State);
		}
		foreach (Player player2 in State.Players)
		{
			ApplyAscensionEffects(player2);
		}
	}

	private void InitializeSavedRun(SerializableRun save)
	{
		foreach (ActModel act in State.Acts)
		{
			act.ValidateRoomsAfterLoad(State.Rng.UpFront);
		}
		AfterMapLocationChanged();
		MapDrawingsToLoad = save.MapDrawings;
		SavedMapsToLoad = null;
		for (int i = 0; i < save.Acts.Count; i++)
		{
			SerializableActMap savedMap = save.Acts[i].SavedMap;
			if (savedMap != null)
			{
				if (SavedMapsToLoad == null)
				{
					Dictionary<int, SerializableActMap> dictionary = (SavedMapsToLoad = new Dictionary<int, SerializableActMap>());
				}
				SavedMapsToLoad[i] = savedMap;
			}
		}
		foreach (ModifierModel modifier in State.Modifiers)
		{
			modifier.OnRunLoaded(State);
		}
	}

	private void SendPostActionChecksum(GameAction action)
	{
		if (CombatManager.Instance.IsInProgress && ((!(action is EndPlayerTurnAction) && !(action is ReadyToBeginEnemyTurnAction)) || 1 == 0))
		{
			ChecksumTracker.GenerateChecksum($"finished executing action {action}", action);
		}
	}

	private void SetStartedWithNeowFlag()
	{
		State.ExtraFields.StartedWithNeow = true;
	}

	public static SerializableRun CanonicalizeSave(SerializableRun save, ulong localPlayerId)
	{
		if (save.Players.FirstOrDefault((SerializablePlayer p) => p.NetId == localPlayerId) == null)
		{
			throw new InvalidOperationException($"Save is invalid! Players does not contain local player Id. IDs in save file: {string.Join(",", save.Players.Select((SerializablePlayer p) => p.NetId))}. Local ID: {localPlayerId}.");
		}
		RunState runState = RunState.FromSerializable(save);
		int latestSchemaVersion = SaveManager.Instance.GetLatestSchemaVersion<SerializableRun>();
		SerializableRun serializableRun = new SerializableRun
		{
			SchemaVersion = latestSchemaVersion,
			Acts = runState.Acts.Zip(save.Acts, delegate(ActModel act, SerializableActModel savedAct)
			{
				SerializableActModel serializableActModel = act.ToSave();
				serializableActModel.SavedMap = savedAct.SavedMap;
				return serializableActModel;
			}).ToList(),
			Modifiers = runState.Modifiers.Select((ModifierModel m) => m.ToSerializable()).ToList(),
			DailyTime = save.DailyTime,
			GameMode = runState.GameMode,
			CurrentActIndex = runState.CurrentActIndex,
			EventsSeen = runState.VisitedEventIds.ToList(),
			SerializableOdds = runState.Odds.ToSerializable(),
			SerializableSharedRelicGrabBag = runState.SharedRelicGrabBag.ToSerializable(),
			Players = runState.Players.Select((Player p) => p.ToSerializable()).ToList(),
			SerializableRng = runState.Rng.ToSerializable(),
			VisitedMapCoords = runState.VisitedMapCoords.ToList(),
			MapPointHistory = runState.MapPointHistory.Select((IReadOnlyList<MapPointHistoryEntry> l) => l.ToList()).ToList(),
			SaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
			StartTime = save.StartTime,
			RunTime = save.RunTime,
			WinTime = save.WinTime,
			Ascension = runState.AscensionLevel,
			PlatformType = save.PlatformType,
			MapDrawings = save.MapDrawings,
			ExtraFields = runState.ExtraFields.ToSerializable(),
			PreFinishedRoom = save.PreFinishedRoom
		};
		PacketWriter packetWriter = new PacketWriter();
		packetWriter.Write(serializableRun);
		return serializableRun;
	}

	public static HashSet<RoomType> BuildRoomTypeBlacklist(MapPointHistoryEntry? previousMapPointEntry, IReadOnlyCollection<MapPoint> nextMapPoints)
	{
		HashSet<RoomType> hashSet = new HashSet<RoomType>();
		if ((previousMapPointEntry != null && previousMapPointEntry.HasRoomOfType(RoomType.Shop)) || (nextMapPoints.Count > 0 && nextMapPoints.All((MapPoint p) => p.PointType == MapPointType.Shop)))
		{
			hashSet.Add(RoomType.Shop);
		}
		return hashSet;
	}

	public SerializableRun ToSave(AbstractRoom? preFinishedRoom)
	{
		int latestSchemaVersion = SaveManager.Instance.GetLatestSchemaVersion<SerializableRun>();
		List<SerializableActModel> list = new List<SerializableActModel>();
		for (int i = 0; i < State.Acts.Count; i++)
		{
			SerializableActModel serializableActModel = State.Acts[i].ToSave();
			if (i == State.CurrentActIndex && State.Map != null)
			{
				serializableActModel.SavedMap = SerializableActMap.FromActMap(State.Map);
			}
			list.Add(serializableActModel);
		}
		return new SerializableRun
		{
			SchemaVersion = latestSchemaVersion,
			Acts = list,
			Modifiers = State.Modifiers.Select((ModifierModel m) => m.ToSerializable()).ToList(),
			DailyTime = DailyTime,
			CurrentActIndex = State.CurrentActIndex,
			EventsSeen = State.VisitedEventIds.ToList(),
			GameMode = State.GameMode,
			SerializableOdds = State.Odds.ToSerializable(),
			SerializableSharedRelicGrabBag = State.SharedRelicGrabBag.ToSerializable(),
			Players = State.Players.Select((Player p) => p.ToSerializable()).ToList(),
			SerializableRng = State.Rng.ToSerializable(),
			VisitedMapCoords = State.VisitedMapCoords.ToList(),
			MapPointHistory = State.MapPointHistory.Select((IReadOnlyList<MapPointHistoryEntry> l) => l.ToList()).ToList(),
			SaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
			StartTime = _startTime,
			RunTime = RunTime,
			WinTime = WinTime,
			Ascension = State.AscensionLevel,
			PlatformType = NetService.Platform,
			MapDrawings = MapDrawingsToLoad,
			ExtraFields = State.ExtraFields.ToSerializable(),
			PreFinishedRoom = preFinishedRoom?.ToSerializable()
		};
	}

	public RunState Launch()
	{
		LocalContext.NetId = NetService.NetId;
		this.RunStarted?.Invoke(State);
		UpdateRichPresence();
		return State;
	}

	public void FinalizeStartingRelics()
	{
		if (State == null)
		{
			return;
		}
		foreach (Player player in State.Players)
		{
			foreach (RelicModel relic in player.Relics)
			{
				relic.AfterObtained();
			}
		}
	}

	public void GenerateRooms()
	{
		List<AncientEventModel> list = State.UnlockState.SharedAncients.ToList().UnstableShuffle(State.Rng.UpFront);
		foreach (ActModel item in State.Acts.Skip(1))
		{
			int count = State.Rng.UpFront.NextInt(list.Count + 1);
			List<AncientEventModel> list2 = list.Take(count).ToList();
			list = list.Except(list2).ToList();
			item.SetSharedAncientSubset(list2);
		}
		for (int i = 0; i < State.Acts.Count; i++)
		{
			ActModel act = State.Acts[i];
			act.GenerateRooms(State.Rng.UpFront, State.UnlockState, State.Players.Count > 1);
			if (ShouldApplyTutorialModifications())
			{
				act.ApplyDiscoveryOrderModifications(State.UnlockState);
			}
			if (i == State.Acts.Count - 1 && AscensionManager.HasLevel(AscensionLevel.DoubleBoss))
			{
				EncounterModel secondBossEncounter = State.Rng.UpFront.NextItem(act.AllBossEncounters.Where((EncounterModel e) => e.Id != act.BossEncounter.Id));
				act.SetSecondBossEncounter(secondBossEncounter);
			}
		}
	}

	public bool ShouldApplyTutorialModifications()
	{
		if (ForceDiscoveryOrderModifications)
		{
			return true;
		}
		if (TestMode.IsOn)
		{
			return false;
		}
		if (State == null)
		{
			return false;
		}
		if (State.GameMode != GameMode.Standard)
		{
			return false;
		}
		return true;
	}

	public void GenerateMap()
	{
		if (State == null)
		{
			throw new InvalidOperationException("State is not set.");
		}
		MapSelectionSynchronizer.BeforeMapGenerated();
		ActMap map;
		if (SavedMapsToLoad != null && SavedMapsToLoad.TryGetValue(State.CurrentActIndex, out SerializableActMap value))
		{
			map = new SavedActMap(value);
			SavedMapsToLoad.Remove(State.CurrentActIndex);
			if (SavedMapsToLoad.Count == 0)
			{
				SavedMapsToLoad = null;
			}
			map = Hook.ModifyGeneratedMapLate(State, map, State.CurrentActIndex);
			Hook.AfterMapGenerated(State, map, State.CurrentActIndex);
		}
		else
		{
			ActMap map2 = State.Act.CreateMap(State, replaceTreasureWithElites: false);
			map = Hook.ModifyGeneratedMap(State, map2, State.CurrentActIndex);
			Hook.AfterMapGenerated(State, map, State.CurrentActIndex);
			if (!State.ExtraFields.StartedWithNeow && State.CurrentActIndex == 0)
			{
				map.StartingMapPoint.PointType = MapPointType.Monster;
			}
		}
		State.Map = map;
		State.RemoveStaleVisitedMapCoords(map);
	}

	public void EnterMapCoord(MapCoord coord)
	{
		if (State == null)
		{
			return;
		}
		if (!State.AddVisitedMapCoord(coord))
		{
			return;
		}
		EnterMapCoordInternal(coord, null, saveGame: true);
		return;
	}

	public void LoadIntoLatestMapCoord(AbstractRoom? preFinishedRoom)
	{
		if (State != null)
		{
			if (State.VisitedMapCoords.Count > 0)
			{
				RunManager runManager = this;
				IReadOnlyList<MapCoord> visitedMapCoords = State.VisitedMapCoords;
				runManager.EnterMapCoordInternal(visitedMapCoords[visitedMapCoords.Count - 1], preFinishedRoom, saveGame: false);
			}
			else
			{
				EnterRoomInternal(new MapRoom());
			}
		}
	}

	private void EnterMapCoordInternal(MapCoord coord, AbstractRoom? preFinishedRoom, bool saveGame)
	{
		if (State == null)
		{
			return;
		}
		MapPoint point = State.Map.GetPoint(coord);
		EnterMapPointInternal(coord.row + 1, point.PointType, preFinishedRoom, saveGame);
		return;
	}

	public void EnterMapPointInternal(int actFloor, MapPointType pointType, AbstractRoom? preFinishedRoom, bool saveGame)
	{
		if (State == null)
		{
			return;
		}
		using (new NetLoadingHandle(NetService))
		{
			if (State.MapPointHistory.Count > 0)
			{
				UpdatePlayerStatsInMapPointHistory();
			}
			State.ActFloor = actFloor;
			ExitCurrentRooms();
			if (preFinishedRoom == null)
			{
				CombatStateSynchronizer.StartSync();
			}
			if (preFinishedRoom == null)
			{
				CombatStateSynchronizer.WaitForSync().GetAwaiter().GetResult();
			}
			if (saveGame)
			{
				SaveManager.Instance.SaveRun(null);
			}
			if (CombatReplayWriter.IsEnabled)
			{
				CombatReplayWriter.RecordInitialState(ToSave(null));
			}
			RoomType roomType;
			if (pointType == MapPointType.Unknown && preFinishedRoom != null)
			{
				roomType = RoomType.Monster;
			}
			else
			{
				HashSet<RoomType> blacklist = BuildRoomTypeBlacklist(State.CurrentMapPointHistoryEntry, State.CurrentMapPoint?.Children ?? new HashSet<MapPoint>());
				roomType = RollRoomTypeFor(pointType, blacklist);
			}
			AbstractRoom abstractRoom = ((preFinishedRoom == null) ? CreateRoom(roomType, pointType) : preFinishedRoom);
			ActionExecutor.Pause();
			if (preFinishedRoom == null)
			{
				State.AppendToMapPointHistory(pointType, abstractRoom.RoomType, abstractRoom.ModelId);
			}
			if (abstractRoom is CombatRoom { IsPreFinished: not false, ParentEventId: not null } combatRoom)
			{
				EventRoom room = new EventRoom(ModelDb.GetById<EventModel>(combatRoom.ParentEventId));
				EnterRoomInternal(room, isRestoringRoomStackBase: true);
				EnterRoomInternal(combatRoom);
			}
			else
			{
				EnterRoom(abstractRoom);
			}
			AfterMapLocationChanged();
		}
	}

	private AbstractRoom CreateRoom(RoomType roomType, MapPointType mapPointType = MapPointType.Unassigned, AbstractModel? model = null)
	{
		if (State == null)
		{
			throw new InvalidOperationException("RunState is not set.");
		}
		switch (roomType)
		{
		case RoomType.Monster:
		case RoomType.Elite:
		case RoomType.Boss:
			return new CombatRoom((model as EncounterModel) ?? State.Act.PullNextEncounter(roomType).ToMutable(), State);
		case RoomType.Treasure:
			return new TreasureRoom(State.CurrentActIndex);
		case RoomType.Shop:
			return new MerchantRoom();
		case RoomType.Event:
			return new EventRoom((model as EventModel) ?? ((mapPointType == MapPointType.Ancient) ? State.Act.PullAncient() : State.Act.PullNextEvent(State)));
		case RoomType.RestSite:
			return new RestSiteRoom();
		case RoomType.Map:
			return new MapRoom();
		default:
			throw new InvalidOperationException($"Unexpected RoomType: {roomType}");
		}
	}

	private RoomType RollRoomTypeFor(MapPointType pointType, IEnumerable<RoomType> blacklist)
	{
		return pointType switch
		{
			MapPointType.Unassigned => RoomType.Unassigned, 
			MapPointType.Unknown => State.Odds.UnknownMapPoint.Roll(blacklist, State), 
			MapPointType.Shop => RoomType.Shop, 
			MapPointType.Treasure => RoomType.Treasure, 
			MapPointType.RestSite => RoomType.RestSite, 
			MapPointType.Monster => RoomType.Monster, 
			MapPointType.Elite => RoomType.Elite, 
			MapPointType.Boss => RoomType.Boss, 
			MapPointType.Ancient => RoomType.Event, 
			_ => throw new ArgumentOutOfRangeException("pointType", pointType, null), 
		};
	}

	private bool TryGetRoomTypeForTutorial(MapPointType pointType, out RoomType roomType)
	{
		roomType = RoomType.Unassigned;
		return false;
	}

	public void EnterMapCoordDebug(MapCoord coord, RoomType roomType, MapPointType pointType = MapPointType.Unassigned, AbstractModel? model = null, bool showTransition = true)
	{
		State.AddVisitedMapCoord(coord);
		EnterRoomDebug(roomType, pointType, model, showTransition);
	}

	public AbstractRoom EnterRoomDebug(RoomType roomType, MapPointType pointType = MapPointType.Unassigned, AbstractModel? model = null, bool showTransition = true)
	{
		using (new NetLoadingHandle(NetService))
		{
			CombatStateSynchronizer.StartSync();
			if (model is EncounterModel encounterModel)
			{
				roomType = encounterModel.RoomType;
			}
			else if (model is EventModel)
			{
				roomType = RoomType.Event;
			}
			if (pointType == MapPointType.Unassigned)
			{
				MapPointType mapPointType = default(MapPointType);
				switch (roomType)
				{
				case RoomType.Monster:
					mapPointType = MapPointType.Monster;
					break;
				case RoomType.Elite:
					mapPointType = MapPointType.Elite;
					break;
				case RoomType.Boss:
					mapPointType = MapPointType.Boss;
					break;
				case RoomType.Treasure:
					mapPointType = MapPointType.Treasure;
					break;
				case RoomType.Shop:
					mapPointType = MapPointType.Shop;
					break;
				case RoomType.Event:
					mapPointType = MapPointType.Unknown;
					break;
				case RoomType.RestSite:
					mapPointType = MapPointType.RestSite;
					break;
				case RoomType.Unassigned:
					mapPointType = MapPointType.Unassigned;
					break;
				case RoomType.Map:
					mapPointType = MapPointType.Unassigned;
					break;
				default:
					throw new System.Runtime.CompilerServices.SwitchExpressionException(roomType);
					break;
				}
				pointType = mapPointType;
			}
			if (CombatReplayWriter.IsEnabled)
			{
				CombatReplayWriter.RecordInitialState(ToSave(null));
			}
			State.AppendToMapPointHistory(pointType, roomType, model?.Id);
			if (State.Map is MockActMap mockActMap)
			{
				mockActMap.MockCurrentMapPointType(pointType);
			}
			CombatStateSynchronizer.WaitForSync().GetAwaiter().GetResult();
			AbstractRoom room = CreateRoom(roomType, MapPointType.Unassigned, model);
			EnterRoom(room);
			return room;
		}
	}

	private void ExitCurrentRooms()
	{
		if (State != null)
		{
			while (State.CurrentRoomCount > 0)
			{
				ExitCurrentRoom();
			}
		}
	}

	private AbstractRoom? ExitCurrentRoom()
	{
		if (State == null)
		{
			return null;
		}
		AbstractRoom currentRoom = State.PopCurrentRoom();
		currentRoom.Exit(State);
		this.RoomExited?.Invoke();
		return currentRoom;
	}

	private void EnterRoomInternal(AbstractRoom room, bool isRestoringRoomStackBase = false)
	{
		if (State == null)
		{
			return;
		}
		bool flag = isRestoringRoomStackBase;
		bool flag2 = flag;
		bool flag3;
		if (!flag2)
		{
			if (room is CombatRoom combatRoom)
			{
				if (combatRoom.IsPreFinished)
				{
					goto IL_0072;
				}
			}
			else if (room is EventRoom { IsPreFinished: not false })
			{
				goto IL_0072;
			}
			flag3 = false;
			goto IL_007a;
		}
		goto IL_007d;
		IL_007d:
		bool runExternalEffects = !flag2;
		State.PushRoom(room);
		if (runExternalEffects && !(room is MapRoom))
		{
			Hook.BeforeRoomEntered(State, room);
		}
		room.Enter(State, isRestoringRoomStackBase);
		if (runExternalEffects)
		{
			if (State.CurrentRoomCount == 1)
			{
				State.Act.MarkRoomVisited(room.RoomType);
			}
		}
		RunLocationTargetedBuffer.OnLocationChanged(State.RunLocation);
		if (!(room is CombatRoom))
		{
			ActionExecutor.Unpause();
		}
		this.RoomEntered?.Invoke();
		return;
		IL_007a:
		flag2 = flag3;
		goto IL_007d;
		IL_0072:
		flag3 = true;
		goto IL_007a;
	}

	public void EnterRoom(AbstractRoom room)
	{
		ExitCurrentRooms();
		EnterRoomInternal(room);
	}

	public void EnterRoomWithoutExitingCurrentRoom(AbstractRoom room, bool fadeToBlack)
	{
		if (State == null)
		{
			return;
		}
		ActionExecutor.Pause();
		CombatStateSynchronizer.StartSync();
		using (new NetLoadingHandle(NetService))
		{
			CombatStateSynchronizer.WaitForSync().GetAwaiter().GetResult();
			State.CurrentMapPointHistoryEntry?.Rooms.Add(new MapPointRoomHistoryEntry
			{
				RoomType = room.RoomType,
				ModelId = room.ModelId
			});
			EnterRoomInternal(room);
		}
	}

	public void EnterNextAct()
	{
		if (State == null)
		{
			return;
		}
		using (new NetLoadingHandle(NetService))
		{
			if (State.CurrentActIndex >= State.Acts.Count - 1)
			{
				WinRun();
			}
			else
			{
				EnterAct(State.CurrentActIndex + 1);
			}
		}
	}

	private void WinRun()
	{
		if (State != null)
		{
			OnEnded(isVictory: true);
			GuaranteeKillAllPlayers();
		}
	}

	public void EnterAct(int currentActIndex, bool doTransition = true)
	{
		if (State == null)
		{
			return;
		}
		using (new NetLoadingHandle(NetService))
		{
			ExitCurrentRooms();
			SetActInternal(currentActIndex);
			_ = doTransition;
			EnterRoomInternal(new MapRoom());
			this.ActEntered?.Invoke();
			Hook.AfterActEntered(State);
		}
	}

	public void SetActInternal(int actIndex)
	{
		if (State != null)
		{
			State.CurrentActIndex = actIndex;
			State.ClearVisitedMapCoordsDebug();
			State.Odds.UnknownMapPoint.ResetToBase();
			AfterMapLocationChanged();
			GenerateMap();
			UpdateRichPresence();
		}
	}

	private void UpdateRichPresence()
	{
	}

	public void ProceedFromTerminalRewardsScreen()
	{
		if (State == null)
		{
			return;
		}
		if (State.CurrentRoomCount > 1)
		{
			if (State.CurrentRoom is CombatRoom { ShouldResumeParentEventAfterCombat: not false })
			{
				ResumePreviousRoom();
				return;
			}
			ExitCurrentRoom();
		}
	}

	private void ResumePreviousRoom()
	{
		if (State != null)
		{
			AbstractRoom abstractRoom = ExitCurrentRoom();
			if (abstractRoom != null)
			{
				State.CurrentRoom.Resume(abstractRoom, State);
			}
			else
			{
				Log.Error("Current room returned null while exiting.");
			}
		}
	}

	private void AfterMapLocationChanged()
	{
		MapSelectionSynchronizer.OnLocationChanged(State.MapLocation);
		RunLocationTargetedBuffer.OnLocationChanged(State.RunLocation);
	}

	public void Abandon()
	{
		Log.Info("Abandoning an in-progress run (player-initiated)");
		AbandonInternal();
	}

	private void AbandonInternal()
	{
		IsAbandoned = true;
		GuaranteeKillAllPlayers();
	}

	private void GuaranteeKillAllPlayers()
	{
		if (State == null)
		{
			return;
		}
		foreach (Player player in State.Players)
		{
			CreatureCmd.Kill(player.Creature, force: true);
			Cmd.CustomScaledWait(0.25f, 0.5f);
		}
	}

	private void StateDiverged(NetFullCombatState state)
	{
		if (NetService.Type != NetGameType.Replay)
		{
			Log.Info("Abandoning run and returning to main menu because our state diverged from host's");
			WriteReplay(stopRecording: false);
		}
	}

	public void WriteReplay(bool stopRecording)
	{
		string profileScopedPath = SaveManager.Instance.GetProfileScopedPath("replays/latest.mcr");
		CombatReplayWriter.WriteReplay(profileScopedPath, stopRecording);
	}

	public void CleanUp(bool graceful = true)
	{
		if (State == null)
		{
			return;
		}
		ShouldSave = false;
		IsCleaningUp = true;
		try
		{
			_runHistoryWasUploaded = false;
			ActionQueueSet.Reset();
			CardSelectCmd.Reset();
			CombatManager.Instance.Reset(graceful);
			ActionExecutor.JustBeforeActionFinishedExecuting -= SendPostActionChecksum;
			CombatReplayWriter.Dispose();
			ActionQueueSynchronizer.Dispose();
			PlayerChoiceSynchronizer.Dispose();
			RewardSynchronizer.Dispose();
			RestSiteSynchronizer.Dispose();
			FlavorSynchronizer.Dispose();
			ChecksumTracker.Dispose();
			NetService.Disconnect(NetError.Quit, !graceful);
		}
		finally
		{
			IsCleaningUp = false;
			LocalContext.NetId = null;
			State = null;
		}
	}

	public SerializableRun OnEnded(bool isVictory)
	{
		UpdatePlayerStatsInMapPointHistory();
		RunState state = State;
		Player me = LocalContext.GetMe(state);
		if (state.CurrentRoom is CombatRoom combatRoom)
		{
			state.CurrentMapPointHistoryEntry.Rooms.Last().TurnsTaken = combatRoom.CombatState.RoundNumber;
		}
		SerializableRun serializableRun = ToSave(null);
		SerializablePlayer me2 = LocalContext.GetMe(serializableRun);
		if (_runHistoryWasUploaded)
		{
			return serializableRun;
		}
		_runHistoryWasUploaded = true;
		if (!isVictory && state.CurrentRoom is CombatRoom combatRoom2)
		{
			foreach (var monstersWithSlot in combatRoom2.Encounter.MonstersWithSlots)
			{
				MonsterModel item = monstersWithSlot.Item1;
				CheckUpdateEnemyDiscoveryAfterLoss(me, item.Id);
			}
		}
		if (ShouldSave)
		{
			using (SaveManager.Instance.BeginSaveBatch())
			{
				SaveManager.Instance.UpdateProgressWithRunData(serializableRun, isVictory);
				foreach (string discoveredEpoch in me2.DiscoveredEpochs)
				{
					if (!me.DiscoveredEpochs.Contains(discoveredEpoch))
					{
						me.DiscoveredEpochs.Add(discoveredEpoch);
					}
				}
				AchievementsHelper.AfterRunEnded(state, me, isVictory);
				if (NetService.Type == NetGameType.Singleplayer)
				{
					SaveManager.Instance.DeleteCurrentRun();
				}
				else if (NetService.Type == NetGameType.Host)
				{
					SaveManager.Instance.DeleteCurrentMultiplayerRun();
				}
			}
			if (isVictory)
			{
				int score = ScoreUtility.CalculateScore(serializableRun, isVictory);
				StatsManager.IncrementArchitectDamage(score);
			}
		}
		return serializableRun;
	}

	private static void CheckUpdateEnemyDiscoveryAfterLoss(Player player, ModelId monster)
	{
		EnemyStats value;
		EnemyStats enemyStats = (SaveManager.Instance.Progress.EnemyStats.TryGetValue(monster, out value) ? value : null);
		if (enemyStats == null)
		{
			player.DiscoveredEnemies.Add(monster);
		}
	}

	private void UpdatePlayerStatsInMapPointHistory()
	{
		if (TestMode.IsOn || State == null)
		{
			return;
		}
		foreach (Player player in State.Players)
		{
			PlayerMapPointHistoryEntry playerMapPointHistoryEntry = State.CurrentMapPointHistoryEntry?.GetEntry(player.NetId);
			if (playerMapPointHistoryEntry != null)
			{
				playerMapPointHistoryEntry.CurrentGold = player.Gold;
				playerMapPointHistoryEntry.CurrentHp = player.Creature.CurrentHp;
				playerMapPointHistoryEntry.MaxHp = player.Creature.MaxHp;
			}
		}
	}

	public bool HasAscension(AscensionLevel level)
	{
		if (!IsInProgress)
		{
			return false;
		}
		return AscensionManager.HasLevel(level);
	}

	public void ApplyAscensionEffects(Player player)
	{
		AscensionManager.ApplyEffectsTo(player);
	}

	public string? GetLocalCharacterEnergyIconPrefix()
	{
		CardPoolModel cardPoolModel = LocalContext.GetMe(State)?.Character.CardPool;
		if (cardPoolModel != null)
		{
			return EnergyIconHelper.GetPrefix(cardPoolModel);
		}
		return null;
	}

	public RunState? DebugOnlyGetState()
	{
		return State;
	}
}
