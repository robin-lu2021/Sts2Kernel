using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core;

/*
This file contains the kernel-side event foundation.

The goal is to preserve the gameplay-facing event state machine from EventModel
while removing scene creation, node binding, VFX, room visuals, and mandatory
async orchestration. The kernel event base keeps:
1. deterministic owner-bound event initialization
2. current description and option state
3. option execution and page transitions
4. cleanup / finished-state handling
5. a lightweight "combat requested" handoff for events that branch into combat

To make the bulk migration from kernel/Events practical, the base still speaks
in terms of the core Player / command-side domain objects. That mirrors what
CardModel / RelicModel / PotionModel already do today: the model itself
is kernel-owned, but it can still interact with the existing core gameplay
commands without pulling GUI code back in.
*/

public sealed class myEventCombatRequest
{
	public EncounterModel Encounter { get; }

	public IReadOnlyList<Reward> ExtraRewards { get; }

	public bool ShouldResumeAfterCombat { get; }

	public myEventCombatRequest(EncounterModel encounter, IEnumerable<Reward>? extraRewards = null, bool shouldResumeAfterCombat = false)
	{
		Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
		ExtraRewards = (extraRewards ?? Array.Empty<Reward>()).ToList();
		ShouldResumeAfterCombat = shouldResumeAfterCombat;
	}
}

public class myEventOption
{
	private readonly Action? _onChosen;

	private readonly bool _disableOnChosen;

	public string TextKey { get; }

	public LocString Title { get; }

	public LocString Description { get; }

	public bool IsLocked { get; }

	public bool IsProceed { get; }

	public bool WasChosen { get; private set; }

	public bool Chosen => WasChosen;

	public RelicModel? Relic { get; private set; }

	public Func<Player, bool>? WillKillPlayer { get; private set; }

	public bool ShouldSaveChoiceToHistory { get; private set; } = true;

	public bool ShouldSaveVariablesToHistory { get; private set; } = true;

	public LocString HistoryName { get; private set; }

	public event Action<myEventOption>? BeforeChosen;

	public myEventOption(EventModel eventModel, Action? onChosen, LocString title, LocString description, string textKey, bool disableOnChosen = true, bool isProceed = false)
	{
		if (eventModel == null)
		{
			throw new ArgumentNullException(nameof(eventModel));
		}
		TextKey = textKey ?? throw new ArgumentNullException(nameof(textKey));
		_onChosen = onChosen;
		Title = title ?? throw new ArgumentNullException(nameof(title));
		Description = description ?? throw new ArgumentNullException(nameof(description));
		IsLocked = _onChosen == null;
		_disableOnChosen = disableOnChosen;
		IsProceed = isProceed;
		HistoryName = Title;
		eventModel.Owner?.Character.AddDetailsTo(Description);
	}

	public myEventOption(EventModel eventModel, Action? onChosen, LocString title, LocString description, string textKey, object? ignoredUiData)
		: this(eventModel, onChosen, title, description, textKey)
	{
	}

	public myEventOption(EventModel eventModel, Action? onChosen, string textKey, bool disableOnChosen = true, bool isProceed = false)
		: this(eventModel, onChosen, ResolveTitle(eventModel, textKey), ResolveDescription(eventModel, textKey), textKey, disableOnChosen, isProceed)
	{
	}

	public myEventOption(EventModel eventModel, Action? onChosen, string textKey, object? ignoredUiData)
		: this(eventModel, onChosen, textKey)
	{
	}

	public myEventOption(EventModel eventModel, Action? onChosen, string textKey, bool disableOnChosen, bool isProceed, object? ignoredUiData)
		: this(eventModel, onChosen, textKey, disableOnChosen, isProceed)
	{
	}

	public static myEventOption FromRelic(RelicModel relic, EventModel eventModel, Action? onChosen, string textKey)
	{
		if (relic == null)
		{
			throw new ArgumentNullException(nameof(relic));
		}
		if (eventModel == null)
		{
			throw new ArgumentNullException(nameof(eventModel));
		}
		LocString title = eventModel.GetOptionTitle(textKey) ?? LocString.GetIfExists("relics", relic.Id + ".title") ?? eventModel.Title;
		LocString description = eventModel.GetOptionDescription(textKey) ?? LocString.GetIfExists("relics", relic.Id + ".description") ?? eventModel.InitialDescription;
		return new myEventOption(eventModel, onChosen, title, description, textKey).WithRelic(relic);
	}

	public myEventOption WithRelic<TRelic>() where TRelic : RelicModel
	{
		return WithRelic(EventModel.ConvertRelicForDisplay(ModelDb.Relic<TRelic>().ToMutable()));
	}

	public myEventOption WithRelic(RelicModel relic)
	{
		Relic = relic ?? throw new ArgumentNullException(nameof(relic));
		return this;
	}

	public void Choose()
	{
		if (_onChosen == null)
		{
			throw new InvalidOperationException($"Event option '{TextKey}' is locked.");
		}
		if (_disableOnChosen && WasChosen)
		{
			return;
		}
		WasChosen = true;
		BeforeChosen?.Invoke(this);
		_onChosen();
	}

	public myEventOption WithOverriddenHistoryName(LocString historyName)
	{
		HistoryName = historyName ?? throw new ArgumentNullException(nameof(historyName));
		return this;
	}

	public myEventOption WithOverridenHistoryName(LocString historyName)
	{
		return WithOverriddenHistoryName(historyName);
	}

	public myEventOption ThatDoesDamage(decimal damage)
	{
		return ThatWillKillPlayerIf((Player player) => player.Creature.CurrentHp <= damage);
	}

	public myEventOption ThatDecreasesMaxHp(decimal value)
	{
		return ThatWillKillPlayerIf((Player player) => player.Creature.MaxHp <= value);
	}

	public myEventOption ThatWillKillPlayerIf(Func<Player, bool> willKillPlayer)
	{
		WillKillPlayer = willKillPlayer ?? throw new ArgumentNullException(nameof(willKillPlayer));
		return this;
	}

	public myEventOption ThatHasDynamicTitle()
	{
		ShouldSaveVariablesToHistory = true;
		return this;
	}

	public myEventOption ThatWontSaveToChoiceHistory()
	{
		ShouldSaveChoiceToHistory = false;
		return this;
	}

	public override string ToString()
	{
		return $"myEventOption title: {SafeFormat(Title)} description: {SafeFormat(Description)} textKey: {TextKey}";
	}

	private static LocString ResolveTitle(EventModel eventModel, string textKey)
	{
		return eventModel.GetOptionTitle(textKey) ?? eventModel.Title;
	}

	private static LocString ResolveDescription(EventModel eventModel, string textKey)
	{
		return eventModel.GetOptionDescription(textKey) ?? eventModel.Description ?? eventModel.InitialDescription;
	}

	private static string SafeFormat(LocString locString)
	{
		try
		{
			return locString.GetFormattedText();
		}
		catch
		{
			return $"{locString.LocTable}.{locString.LocEntryKey}";
		}
	}
}

public abstract class EventModel : AbstractModel
{
	private List<myEventOption>? _currentOptions;

	private bool _isFinished;

	private bool _cleanupCalled;

	private DynamicVarSet? _dynamicVars;

	private CombatState? _combatStateForCombatLayout;

	public virtual string Id => GetType().Name;

	public ModelId ModelId => base.Id;

	public virtual string LocTable => "events";

	public virtual EventLayoutType LayoutType => EventLayoutType.Default;

	public virtual bool IsShared => false;

	public virtual bool IsDeterministic => !IsShared;

	public override bool ShouldReceiveCombatHooks => false;

	public virtual EncounterModel? CanonicalEncounter => null;

	public virtual IEnumerable<string> MapNodeAssetPaths => Array.Empty<string>();

	public Player? Owner { get; private set; }

	public LocString Title => L10NLookup(ModelId.Entry + ".title");

	public virtual LocString InitialDescription => L10NLookup(ModelId.Entry + ".pages.INITIAL.description");

	public virtual IEnumerable<LocString> GameInfoOptions
	{
		get
		{
			LocTable? table = LocManager.Instance.GetTable(LocTable);
			if (table == null)
			{
				return Array.Empty<LocString>();
			}
			List<LocString> result = (from key in table.Keys
				where key.StartsWith(ModelId.Entry + ".pages.INITIAL.options", StringComparison.Ordinal)
				select new LocString(LocTable, key)).ToList();
			foreach (LocString item in result)
			{
				DynamicVars.AddTo(item);
			}
			return result;
		}
	}

	public LocString? Description { get; private set; }

	public bool IsFinished
	{
		get => _isFinished;
		private set
		{
			AssertMutable();
			_isFinished = value;
		}
	}

	public IReadOnlyList<myEventOption> CurrentOptions
	{
		get
		{
			if (_currentOptions == null)
			{
				_currentOptions = new List<myEventOption>();
			}
			return _currentOptions;
		}
	}

	public DynamicVarSet DynamicVars
	{
		get
		{
			if (_dynamicVars != null)
			{
				return _dynamicVars;
			}
			_dynamicVars = new DynamicVarSet(CanonicalVars);
			_dynamicVars.InitializeWithOwner(this);
			return _dynamicVars;
		}
	}

	protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

	public Rng Rng { get; private set; } = Rng.Chaotic;

	public myEventCombatRequest? PendingCombatRequest { get; private set; }

	public event Action<EventModel>? StateChanged;

	public event Action<myEventCombatRequest>? CombatRequested;

	public LocString? GetOptionTitle(string key)
	{
		return LocString.GetIfExists(LocTable, key + ".title");
	}

	public LocString? GetOptionDescription(string key)
	{
		return LocString.GetIfExists(LocTable, key + ".description");
	}

	public void BeginEvent(Player owner, bool isPreFinished)
	{
		if (Owner != null)
		{
			throw new InvalidOperationException($"Event '{Id}' has already been started.");
		}
		Owner = owner ?? throw new ArgumentNullException(nameof(owner));
		uint ownerSeed = IsShared ? 0u : unchecked((uint)Owner.NetId);
		uint runSeed = unchecked((uint)Owner.RunState.Rng.Seed);
		uint hashEntry = unchecked((uint)StringHelper.GetDeterministicHashCode(ModelId.Entry));
		uint eventSeed = runSeed + ownerSeed + hashEntry;
		Rng = new Rng(eventSeed);
		try
		{
			BeforeEventStarted(isPreFinished);
			CalculateVars();
			if (owner.Creature.IsDead)
			{
				SetEventFinished(L10NLookup("GENERIC.youAreDead.description"));
			}
			else
			{
				SetInitialEventState(isPreFinished);
			}
		}
		catch
		{
			EnsureCleanup();
			throw;
		}
	}

	public void ChooseOption(int optionIndex)
	{
		if (optionIndex < 0 || optionIndex >= CurrentOptions.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(optionIndex));
		}
		ChooseOption(CurrentOptions[optionIndex]);
	}

	public void ChooseOption(myEventOption option)
	{
		if (option == null)
		{
			throw new ArgumentNullException(nameof(option));
		}
		if (IsFinished)
		{
			throw new InvalidOperationException($"Event '{Id}' is already finished.");
		}
		option.Choose();
	}

	protected virtual void SetInitialEventState(bool isPreFinished)
	{
		if (isPreFinished)
		{
			throw new InvalidOperationException($"Tried to load event '{Id}' as pre-finished, but this kernel base does not define a pre-finished flow.");
		}
		IReadOnlyList<myEventOption> options = GenerateInitialOptionsWrapper();
		SetEventState(InitialDescription, options);
	}

	protected virtual IReadOnlyList<myEventOption> GenerateInitialOptionsWrapper()
	{
		List<myEventOption> options = GenerateInitialOptions().ToList();
		ReplaceNullOptions(options);
		return options;
	}

	protected abstract IReadOnlyList<myEventOption> GenerateInitialOptions();

	protected void ReplaceNullOptions(List<myEventOption> options)
	{
		if (options == null)
		{
			throw new ArgumentNullException(nameof(options));
		}
		for (int i = 0; i < options.Count; i++)
		{
			if (options[i] is null)
			{
				options[i] = new myEventOption(this, (Action?)null, "ERROR");
			}
		}
	}

	protected void ClearCurrentOptions()
	{
		if (_currentOptions == null)
		{
			_currentOptions = new List<myEventOption>();
		}
		_currentOptions.Clear();
	}

	public virtual bool IsAllowed(IRunState runState)
	{
		return true;
	}

	public virtual void CalculateVars()
	{
	}

	public virtual void OnRoomEnter()
	{
	}

	public virtual void Resume(AbstractRoom exitedRoom)
	{
		return;
	}

	protected void SetEventFinished(LocString description)
	{
		SetEventState(description, Array.Empty<myEventOption>());
		IsFinished = true;
		EnsureCleanup();
	}

	protected virtual void BeforeEventStarted(bool isPreFinished)
	{
		return;
	}

	public virtual void AfterEventStarted()
	{
		return;
	}

	public virtual EventModel ToMutable()
	{
		return IsMutable ? this : (EventModel)MutableClone();
	}

	protected override void DeepCloneFields()
	{
		base.DeepCloneFields();
		_dynamicVars = _dynamicVars != null ? DynamicVars.Clone(this) : null;
	}

	protected override void AfterCloned()
	{
		base.AfterCloned();
		StateChanged = null;
		CombatRequested = null;
		Owner = null;
		Description = null;
		PendingCombatRequest = null;
		_currentOptions = null;
		_isFinished = false;
		_cleanupCalled = false;
		Rng = Rng.Chaotic;
	}

	public virtual void GenerateInternalCombatState(IRunState runState)
	{
	}

	public void ResetInternalCombatState()
	{
		if (LayoutType != EventLayoutType.Combat)
		{
			throw new InvalidOperationException("Tried to reset internal encounter for non-combat event!");
		}
		if (_combatStateForCombatLayout == null)
		{
			return;
		}
		foreach (Creature item in _combatStateForCombatLayout.Creatures.ToList())
		{
			_combatStateForCombatLayout.RemoveCreature(item);
		}
		_combatStateForCombatLayout = null;
	}


	protected virtual void OnEventFinished()
	{
	}

	public void EnsureCleanup()
	{
		if (_cleanupCalled)
		{
			return;
		}
		_cleanupCalled = true;
		OnEventFinished();
	}

	protected virtual void SetEventState(LocString description, IEnumerable<myEventOption> eventOptions)
	{
		if (description == null)
		{
			throw new ArgumentNullException(nameof(description));
		}
		if (_currentOptions == null)
		{
			_currentOptions = new List<myEventOption>();
		}
		_currentOptions.Clear();
		_currentOptions.AddRange(eventOptions ?? Array.Empty<myEventOption>());
		Description = description;
		if (_currentOptions.Count == 0)
		{
			if (_isFinished)
			{
				throw new InvalidOperationException("Tried to set event options after event was finished.");
			}
			_isFinished = true;
		}
		StateChanged?.Invoke(this);
	}

	protected void ClearPendingCombatRequest()
	{
		PendingCombatRequest = null;
	}

	public myEventCombatRequest? TakePendingCombatRequest()
	{
		myEventCombatRequest? request = PendingCombatRequest;
		ClearPendingCombatRequest();
		return request;
	}

	protected void RequestCombat(EncounterModel encounter, IEnumerable<Reward>? extraRewards = null, bool shouldResumeAfterCombat = false)
	{
		PendingCombatRequest = new myEventCombatRequest(encounter, extraRewards, shouldResumeAfterCombat);
		CombatRequested?.Invoke(PendingCombatRequest);
	}

	protected void EnterCombatWithoutExitingEvent<TEncounter>(IReadOnlyList<Reward>? extraRewards = null, bool shouldResumeAfterCombat = false)
		where TEncounter : EncounterModel
	{
		RequestCombat(ModelDb.Encounter<TEncounter>().ToMutable(), extraRewards, shouldResumeAfterCombat);
	}

	protected void EnterCombatWithoutExitingEvent(EncounterModel mutableEncounter, IReadOnlyList<Reward>? extraRewards = null, bool shouldResumeAfterCombat = false)
	{
		RequestCombat(mutableEncounter, extraRewards, shouldResumeAfterCombat);
	}

	protected myEventOption RelicOption<TRelic>(Action? onChosen, string pageName = "INITIAL") where TRelic : RelicModel
	{
		return RelicOption(ConvertRelicForDisplay(ModelDb.Relic<TRelic>().ToMutable()), onChosen, pageName);
	}

	protected myEventOption RelicOption(RelicModel relic, Action? onChosen, string pageName = "INITIAL")
	{
		if (relic == null)
		{
			throw new ArgumentNullException(nameof(relic));
		}
		string textKey = OptionKey(pageName, relic.Id);
		return myEventOption.FromRelic(relic, this, onChosen, textKey);
	}

	protected string InitialOptionKey(string optionName)
	{
		return OptionKey("INITIAL", optionName);
	}

	protected string OptionKey(string pageName, string optionName)
	{
		return $"{StringHelper.Slugify(GetType().Name)}.pages.{pageName}.options.{optionName}";
	}

	protected string OptionKey(string pageName, ModelId optionId)
	{
		return OptionKey(pageName, optionId.Entry);
	}

	protected LocString L10NLookup(string entryName)
	{
		return new LocString(LocTable, entryName);
	}

	protected internal static RelicModel ConvertRelicForDisplay(object relicObject)
	{
		return relicObject switch
		{
			RelicModel kernelRelic => kernelRelic,
			_ => throw new InvalidOperationException($"Unsupported relic option type '{relicObject.GetType().FullName}'.")
		};
	}

	protected internal static RelicModel ConvertRelicForCommand(object relicObject)
	{
		return ConvertRelicForDisplay(relicObject);
	}

	protected internal static RelicModel ConvertRelicForDisplay(object relicObject, Action? onChosen, string pageName)
	{
		return ConvertRelicForDisplay(relicObject);
	}

	protected internal static RelicModel ConvertRelicForDisplay(object relicObject, Func<Task>? onChosen, string pageName)
	{
		return ConvertRelicForDisplay(relicObject);
	}
}
