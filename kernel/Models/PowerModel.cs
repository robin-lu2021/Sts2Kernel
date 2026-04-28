using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core;

public sealed class mySerializablePower
{
	public string Id { get; set; } = string.Empty;

	public int Amount { get; set; }

	public int AmountOnTurnStart { get; set; }

	public bool SkipNextDurationTick { get; set; }

	public bool HasBeenRemovedFromState { get; set; }

	public Dictionary<string, string> State { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

public abstract class PowerModel : AbstractModel
{
	public const string LocTable = "powers";

	private int _amount;

	private int _amountOnTurnStart;

	private bool _skipNextDurationTick;

	private Creature? _owner;

	private Creature? _applier;

	private Creature? _target;

	private DynamicVarSet? _dynamicVars;

	private object? _internalData;

	public virtual string ContentId => Id.Entry;

	public virtual LocString Title => new LocString(LocTable, ContentId + ".title");

	public virtual LocString Description => new LocString(LocTable, ContentId + ".description");

	public virtual string? IconPath => null;

	public virtual string? ResolvedBigIconPath => IconPath;

	public LocString SmartDescription
	{
		get
		{
			if (!HasSmartDescription)
			{
				return Description;
			}
			return new LocString(LocTable, SmartDescriptionLocKey);
		}
	}

	public bool HasSmartDescription => LocString.Exists(LocTable, SmartDescriptionLocKey);

	public LocString RemoteDescription
	{
		get
		{
			if (!HasRemoteDescription)
			{
				return Description;
			}
			return new LocString(LocTable, RemoteDescriptionLocKey);
		}
	}

	public bool HasRemoteDescription => LocString.Exists(LocTable, RemoteDescriptionLocKey);

	protected virtual string RemoteDescriptionLocKey => ContentId + ".remoteDescription";

	protected virtual string SmartDescriptionLocKey => ContentId + ".smartDescription";

	protected LocString SelectionScreenPrompt
	{
		get
		{
			LocString locString = new LocString(LocTable, ContentId + ".selectionScreenPrompt");
			if (!locString.Exists())
			{
				throw new InvalidOperationException($"No selection screen prompt for {Id}.");
			}
			DynamicVars.AddTo(locString);
			locString.Add("Amount", Amount);
			return locString;
		}
	}

	public abstract PowerType Type { get; }

	public abstract PowerStackType StackType { get; }

	public virtual bool IsInstanced => false;

	public virtual bool AllowNegative => false;

	public virtual bool ShouldScaleInMultiplayer => false;

	public virtual bool OwnerIsSecondaryEnemy => false;

	public virtual bool ShouldPlayVfx => false;

	protected virtual bool IsVisibleInternal => true;

	public bool IsVisible => IsVisibleInternal;

	public override bool ShouldReceiveCombatHooks => true;

	public virtual bool IsTemporaryPower => false;

	public virtual PowerModel? InternallyAppliedPowerModel => null;

	public virtual void IgnoreNextTemporaryPowerInstance()
	{
	}

	public int Amount
	{
		get
		{
			return _amount;
		}
		set
		{
			SetAmount(value);
		}
	}

	public int AmountOnTurnStart
	{
		get
		{
			return _amountOnTurnStart;
		}
		set
		{
			AssertMutable();
			_amountOnTurnStart = value;
		}
	}

	public virtual int DisplayAmount => Amount;

	public bool SkipNextDurationTick
	{
		get
		{
			return _skipNextDurationTick;
		}
		set
		{
			AssertMutable();
			_skipNextDurationTick = value;
		}
	}

	public Creature Owner
	{
		get
		{
			return _owner ?? throw new InvalidOperationException($"Power '{Id}' does not have an owner yet.");
		}
		private set
		{
			AssertMutable();
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			if (_owner != null && !ReferenceEquals(_owner, value))
			{
				throw new InvalidOperationException("Cannot move power " + Id.Entry + " from one owner to another");
			}
			_owner = value;
		}
	}

	public bool HasOwner => _owner != null;

	public CombatState? CombatState => _owner?.CombatState;

	public Creature? Applier
	{
		get
		{
			return _applier;
		}
		set
		{
			AssertMutable();
			_applier = value;
		}
	}

	public Creature? Target
	{
		get
		{
			return _target;
		}
		set
		{
			AssertMutable();
			_target = value;
		}
	}

	public bool HasBeenRemovedFromState { get; private set; }

	public PowerType TypeForCurrentAmount => GetTypeForAmount(Amount);

	protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

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

	public HoverTip DumbHoverTip
	{
		get
		{
			HoverTip result = new HoverTip(this, GetDescriptionLocString(preferSmartDescription: false).GetFormattedText(), isSmart: false);
			return result;
		}
	}

	protected virtual IEnumerable<IHoverTip> ExtraHoverTips => Array.Empty<IHoverTip>();

	public IEnumerable<IHoverTip> HoverTips
	{
		get
		{
			if (!IsVisible)
			{
				return Array.Empty<IHoverTip>();
			}
			bool useSmartDescription = HasSmartDescription && IsMutable;
			HoverTip hoverTip = new HoverTip(this, GetDescriptionLocString(useSmartDescription, preferRemoteDescription: true).GetFormattedText(), useSmartDescription);
			return new IHoverTip[1] { hoverTip }.Concat(ExtraHoverTips);
		}
	}

	protected virtual object? InitInternalData()
	{
		return null;
	}

	protected T GetInternalData<T>()
	{
		object? internalData = EnsureInternalData();
		if (internalData is T typed)
		{
			return typed;
		}
		if (internalData == null)
		{
			throw new InvalidOperationException($"Power '{Id}' does not have internal data of type '{typeof(T).Name}'.");
		}
		throw new InvalidOperationException($"Power '{Id}' internal data is '{internalData.GetType().Name}', not '{typeof(T).Name}'.");
	}

	private object? EnsureInternalData()
	{
		if (_internalData == null)
		{
			_internalData = InitInternalData();
		}
		return _internalData;
	}

	public LocString GetDescriptionLocString(bool preferSmartDescription = true, bool preferRemoteDescription = false)
	{
		LocString description = preferSmartDescription && HasSmartDescription
			? (preferRemoteDescription && HasRemoteDescription ? RemoteDescription : SmartDescription)
			: Description;
		description.Add("Amount", Amount);
		CombatState? combatState = CombatState;
		if (HasOwner)
		{
			if (combatState != null)
			{
				description.Add("PlayerCount", combatState.Players.Count);
			}
		}
		AddDescriptionVariables(description);
		DynamicVars.AddTo(description);
		return description;
	}

	public string GetFormattedDescription(bool preferSmartDescription = true, bool preferRemoteDescription = false)
	{
		return GetDescriptionLocString(preferSmartDescription, preferRemoteDescription).GetFormattedText();
	}

	protected virtual void AddDescriptionVariables(LocString description)
	{
	}

	private static object GetCreatureDisplayName(Creature creature)
	{
		if (creature.IsPlayer)
		{
			return creature.Player.Character.Title;
		}
		if (creature.IsMonster)
		{
			return creature.Monster.Title;
		}
		return creature.GetType().Name;
	}

	public void Flash()
	{
	}

	public void StartPulsing()
	{
	}

	public void StopPulsing()
	{
	}

	protected void InvokeDisplayAmountChanged()
	{
	}

	protected override void AfterCloned()
	{
		base.AfterCloned();
		_owner = null;
		_applier = null;
		_target = null;
		_amount = 0;
		_amountOnTurnStart = 0;
		_skipNextDurationTick = false;
		HasBeenRemovedFromState = false;
	}

	public PowerType GetTypeForAmount(decimal customAmount)
	{
		if (StackType == PowerStackType.Counter && AllowNegative && customAmount < 0m)
		{
			return PowerType.Debuff;
		}
		if (!AllowNegative && Type == PowerType.Debuff && customAmount < 0m)
		{
			return PowerType.Buff;
		}
		return Type;
	}

	public bool ShouldRemoveDueToAmount()
	{
		if (AllowNegative)
		{
			return Amount == 0;
		}
		return Amount <= 0;
	}

	public void SetAmount(int amount, bool silent = false)
	{
		AssertMutable();
		amount = Math.Clamp(amount, -999999999, 999999999);
		int delta = amount - _amount;
		if (delta == 0)
		{
			return;
		}
		_amount = amount;
		if (!silent)
		{
			InvokeDisplayAmountChanged();
		}
		if (HasOwner)
		{
			Owner.InvokePowerModified(this, delta, silent);
		}
	}

	public virtual PowerModel ToMutable(int initialAmount = 0)
	{
		PowerModel clone = (PowerModel)MutableClone();
		clone.Amount = initialAmount;
		return clone;
	}

	public void ApplyInternal(Creature owner, decimal amount, bool silent = false)
	{
		if (amount == 0m)
		{
			return;
		}
		Owner = owner;
		HasBeenRemovedFromState = false;
		SetAmount((int)amount, silent);
		owner.ApplyPowerInternal(this);
	}

	public void RemoveInternal()
	{
		HasBeenRemovedFromState = true;
		if (HasOwner)
		{
			Owner.RemovePowerInternal(this);
		}
	}

	public mySerializablePower SaveState()
	{
		mySerializablePower save = new mySerializablePower
		{
			Id = ContentId,
			Amount = Amount,
			AmountOnTurnStart = AmountOnTurnStart,
			SkipNextDurationTick = SkipNextDurationTick,
			HasBeenRemovedFromState = HasBeenRemovedFromState
		};
		WriteCustomState(save.State);
		return save;
	}

	public virtual void LoadState(mySerializablePower save)
	{
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		if (!string.IsNullOrWhiteSpace(save.Id) && !string.Equals(save.Id, ContentId, StringComparison.Ordinal))
		{
			throw new InvalidOperationException($"Cannot load power state for '{save.Id}' into '{ContentId}'.");
		}
		_amount = save.Amount;
		_amountOnTurnStart = save.AmountOnTurnStart;
		_skipNextDurationTick = save.SkipNextDurationTick;
		_internalData = null;
		_dynamicVars = null;
		HasBeenRemovedFromState = save.HasBeenRemovedFromState;
		ReadCustomState(save.State);
	}

	public static TPower RestoreState<TPower>(mySerializablePower save)
		where TPower : PowerModel, new()
	{
		TPower power = new TPower();
		power.LoadState(save);
		return power;
	}

	public static PowerModel FromCore(PowerModel sourcePower)
	{
		if (sourcePower == null)
		{
			throw new ArgumentNullException(nameof(sourcePower));
		}

		PowerModel power = TryCreateKernelEquivalent(sourcePower.ContentId) ?? new myWrappedCorePower(sourcePower);
		power.SetAmount(sourcePower.Amount, silent: true);
		power.AmountOnTurnStart = sourcePower.AmountOnTurnStart;
		power.SkipNextDurationTick = sourcePower.SkipNextDurationTick;
		power.Applier = sourcePower.Applier;
		power.Target = sourcePower.Target;

		Creature? owner = TryGetCoreOwner(sourcePower);
		if (owner != null)
		{
			power.Owner = owner;
		}

		return power;
	}

	protected virtual void WriteCustomState(Dictionary<string, string> state)
	{
	}

	protected virtual void ReadCustomState(IReadOnlyDictionary<string, string> state)
	{
	}

	private static PowerModel? TryCreateKernelEquivalent(string powerId)
	{
		foreach (Type type in typeof(PowerModel).Assembly.GetTypes())
		{
			if (type.IsAbstract || !typeof(PowerModel).IsAssignableFrom(type))
			{
				continue;
			}
			if (string.Equals(type.Name, powerId, StringComparison.OrdinalIgnoreCase))
			{
				return ModelDb.DebugPower(type).ToMutable();
			}
		}
		return null;
	}

	protected override void DeepCloneFields()
	{
		base.DeepCloneFields();
		_dynamicVars = _dynamicVars != null ? DynamicVars.Clone(this) : null;
		_internalData = InitInternalData();
	}


	private static Creature? TryGetCoreOwner(PowerModel sourcePower)
	{
		try
		{
			return sourcePower.Owner;
		}
		catch
		{
			return null;
		}
	}

	private sealed class myWrappedCorePower : PowerModel
	{
		private readonly PowerModel _sourcePower;

		public override string ContentId => _sourcePower.ContentId;

		public override LocString Title => _sourcePower.Title;

		public override LocString Description => _sourcePower.Description;

		public override PowerType Type => _sourcePower.Type;

		public override PowerStackType StackType => _sourcePower.StackType;

		public override bool IsInstanced => _sourcePower.IsInstanced;

		public override bool AllowNegative => _sourcePower.AllowNegative;

		public override int DisplayAmount => _sourcePower.DisplayAmount;

		public override bool ShouldScaleInMultiplayer => _sourcePower.ShouldScaleInMultiplayer;

		public override bool OwnerIsSecondaryEnemy => _sourcePower.OwnerIsSecondaryEnemy;

		public override bool ShouldPlayVfx => _sourcePower.ShouldPlayVfx;

		public myWrappedCorePower(PowerModel sourcePower)
		{
			_sourcePower = sourcePower ?? throw new ArgumentNullException(nameof(sourcePower));
		}

		public override bool ShouldPowerBeRemovedAfterOwnerDeath()
		{
			return _sourcePower.ShouldPowerBeRemovedAfterOwnerDeath();
		}

		public override bool ShouldOwnerDeathTriggerFatal()
		{
			return _sourcePower.ShouldOwnerDeathTriggerFatal();
		}
	}

	public override void AfterBlockCleared(Creature creature)
	{
		return;
	}

	public override void AfterBlockBroken(Creature creature)
	{
		return;
	}

	public override void AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
	{
		return;
	}

	public override void BeforeCombatStart()
	{
		return;
	}

	public override void AfterCombatEnd(CombatRoom room)
	{
		return;
	}

	public override void AfterCombatVictoryEarly(CombatRoom room)
	{
		return;
	}

	public override void AfterCombatVictory(CombatRoom room)
	{
		return;
	}

	public override void AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		return;
	}

	public override void AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		return;
	}

	public override void BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return;
	}

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return;
	}

	public override void AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return;
	}

	public override void BeforeDeath(Creature creature)
	{
		return;
	}

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		return;
	}

	public override void AfterDiedToDoom(PlayerChoiceContext choiceContext, IReadOnlyList<Creature> creatures)
	{
		return;
	}

	public override void AfterEnergyReset(Player player)
	{
		return;
	}

	public override void AfterEnergyResetLate(Player player)
	{
		return;
	}

	public override void AfterEnergySpent(CardModel card, int amount)
	{
		return;
	}

	public override void BeforeCardRemoved(CardModel card)
	{
		return;
	}

	public override void BeforeFlush(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public override void BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public override void AfterGoldGained(Player player)
	{
		return;
	}

	public override void BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
	{
		return;
	}

	public override void BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
	{
		return;
	}

	public override void AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay)
	{
		return;
	}

	public override void AfterModifyingCardPlayCount(CardModel card)
	{
		return;
	}

	public override void AfterModifyingCardPlayResultPileOrPosition(CardModel card, PileType pileType, CardPilePosition position)
	{
		return;
	}

	public override void AfterModifyingDamageAmount(CardModel? cardSource)
	{
		return;
	}

	public override void AfterModifyingHpLostBeforeOsty()
	{
		return;
	}

	public override void AfterModifyingHpLostAfterOsty()
	{
		return;
	}

	public override void AfterModifyingPowerAmountReceived(PowerModel power)
	{
		return;
	}

	public override void BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
	{
		return;
	}

	public override void AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
	{
		return;
	}

	public override void AfterPreventingDeath(Creature creature)
	{
		return;
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		return;
	}

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		return;
	}

	public override void AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public override void BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public override void BeforeTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return;
	}

	public override void BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return;
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return;
	}

	public override void AfterTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return;
	}

	public override void AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
	{
		return;
	}

	public override void BeforePotionUsed(PotionModel potion, Creature? target)
	{
		return;
	}

	public override void AfterStarsSpent(int amount, Player spender)
	{
		return;
	}

	public override void AfterStarsGained(int amount, Player gainer)
	{
		return;
	}

	public override void AfterForge(decimal amount, Player forger, AbstractModel? source)
	{
		return;
	}

	public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return 0m;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return 1m;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		return playCount;
	}

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
	{
		return (pileType, position);
	}

	public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 0m;
	}

	public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return decimal.MaxValue;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1m;
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return count;
	}

	public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return amount;
	}

	public override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return amount;
	}

	public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return amount;
	}

	public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return amount;
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return amount;
	}

	public override decimal ModifyOrbValue(Player player, decimal value)
	{
		return value;
	}

	public override decimal ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		return amount;
	}

	public override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer)
	{
		return target;
	}

	public override int ModifyXValue(CardModel card, int originalValue)
	{
		return originalValue;
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		return false;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		return false;
	}

	public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		return false;
	}

	public override bool ShouldPowerBeRemovedOnDeath(PowerModel power)
	{
		return true;
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return false;
	}

	public virtual void BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return;
	}

	public virtual void AfterApplied(Creature? applier, CardModel? cardSource)
	{
		return;
	}

	public virtual void AfterRemoved(Creature oldOwner)
	{
		return;
	}

	public virtual bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return true;
	}

	public virtual bool ShouldOwnerDeathTriggerFatal()
	{
		return true;
	}
}
