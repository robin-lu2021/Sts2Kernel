using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core;

public sealed class mySerializableRelic : IPacketSerializable
{
	public string Id { get; set; } = string.Empty;

	public RelicStatus Status { get; set; } = RelicStatus.Normal;

	public bool HasBeenRemovedFromState { get; set; }

	public int FloorAddedToDeck { get; set; }

	public Dictionary<string, string> State { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

	public MegaCrit.Sts2.Core.Saves.Runs.mySerializableRelic ToRunSerializable()
	{
		return new MegaCrit.Sts2.Core.Saves.Runs.mySerializableRelic
		{
			Id = string.IsNullOrWhiteSpace(Id) ? null : new ModelId("RELIC", Id),
			FloorAddedToDeck = FloorAddedToDeck
		};
	}

	public static implicit operator MegaCrit.Sts2.Core.Saves.Runs.mySerializableRelic(mySerializableRelic save)
	{
		return save?.ToRunSerializable() ?? new MegaCrit.Sts2.Core.Saves.Runs.mySerializableRelic();
	}

	public static implicit operator mySerializableRelic(MegaCrit.Sts2.Core.Saves.Runs.mySerializableRelic save)
	{
		return new mySerializableRelic
		{
			Id = save?.Id?.Entry ?? string.Empty,
			FloorAddedToDeck = save?.FloorAddedToDeck ?? 0
		};
	}

	public void Serialize(PacketWriter writer)
	{
		writer.WriteString(Id);
		writer.WriteInt((int)Status);
		writer.WriteBool(HasBeenRemovedFromState);
		writer.WriteInt(FloorAddedToDeck);
		writer.WriteInt(State.Count);
		foreach (KeyValuePair<string, string> pair in State.OrderBy((KeyValuePair<string, string> kvp) => kvp.Key, StringComparer.Ordinal))
		{
			writer.WriteString(pair.Key);
			writer.WriteString(pair.Value);
		}
	}

	public void Deserialize(PacketReader reader)
	{
		Id = reader.ReadString();
		Status = (RelicStatus)reader.ReadInt();
		HasBeenRemovedFromState = reader.ReadBool();
		FloorAddedToDeck = reader.ReadInt();
		int count = reader.ReadInt();
		State = new Dictionary<string, string>(count, StringComparer.Ordinal);
		for (int i = 0; i < count; i++)
		{
			State[reader.ReadString()] = reader.ReadString();
		}
	}
}

public abstract class RelicModel : AbstractModel
{
	private Player? _owner;

	private DynamicVarSet? _dynamicVars;

	private RelicModel? _canonicalInstance;

	private RelicPoolModel? _pool;

	public virtual string ContentId => Id.Entry;

	public abstract RelicRarity Rarity { get; }

	public virtual bool IsAllowedInShops => true;

	public Player Owner
	{
		get
		{
			return _owner ?? throw new InvalidOperationException($"Relic '{ContentId}' does not have an owner yet.");
		}
		set
		{
			AssertMutable();
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			if (_owner != null && !ReferenceEquals(_owner, value))
			{
				throw new InvalidOperationException("Cannot move relic from " + Id.Entry + " one owner to another");
			}
			_owner = value;
		}
	}

	public virtual bool IsUsedUp => false;

	public virtual bool HasUponPickupEffect => false;

	public virtual bool SpawnsPets => false;

	public virtual bool IsStackable => false;

	public bool HasOwner => _owner != null;

	public virtual bool AddsPet => false;

	public RelicStatus Status { get; set; } = RelicStatus.Normal;

	public bool HasBeenRemovedFromState { get; private set; }

	public int FloorAddedToDeck { get; set; }

	public bool IsWax { get; set; }

	public bool IsMelted { get; set; }

	public virtual bool IsTradable
	{
		get
		{
			if (IsUsedUp || HasUponPickupEffect || IsMelted || SpawnsPets)
			{
				return false;
			}
			return Rarity != RelicRarity.Starter && Rarity != RelicRarity.Event && Rarity != RelicRarity.Ancient;
		}
	}

	public virtual bool ShouldFlashOnPlayer => false;

	public virtual bool ShowCounter => false;

	public virtual int DisplayAmount => 0;

	public virtual IPoolModel Pool
	{
		get
		{
			if (_pool != null)
			{
				return _pool;
			}
			foreach (RelicPoolModel pool in ModelDb.AllRelicPools)
			{
				if (pool.AllRelicIds.Contains(Id))
				{
					_pool = pool;
					return _pool;
				}
			}
			throw new InvalidProgramException($"Relic {this} is not in any relic pool!");
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

	public RelicModel CanonicalInstance => !IsMutable ? this : _canonicalInstance ?? this;

	public virtual string? IconPath => null;

	protected bool CanRunHooks => !HasBeenRemovedFromState && Status != RelicStatus.Disabled;

	public override bool ShouldReceiveCombatHooks => true;
	
	public virtual int MerchantCost => Rarity switch
	{
		RelicRarity.Common => 175, 
		RelicRarity.Uncommon => 225, 
		RelicRarity.Rare => 275, 
		RelicRarity.Shop => 200, 
		RelicRarity.Ancient => 999999999, 
		RelicRarity.Starter => 999999999, 
		RelicRarity.Event => 999999999, 
		RelicRarity.None => 1,
		_ => 999
	};

	protected LocString? AdditionalRestSiteHealText => null;

	public static LocString L10NLookup(string key)
	{
		return new LocString("relics", key);
	}
	
	public virtual LocString Title
	{
		get
		{
			LocString locString = new LocString("relics", ContentId + ".title");
			if (IsWax)
			{
				LocString waxRelicPrefix = MegaCrit.Sts2.Core.Models.Relics.ToyBox.WaxRelicPrefix;
				waxRelicPrefix.Add("Title", locString);
				locString = waxRelicPrefix;
			}
			return locString;
		}
	}

	private LocString Description => new LocString("relics", base.Id.Entry + ".description");

	public virtual LocString DynamicDescription
	{
		get
		{
			LocString description = Description;
			DynamicVars.AddTo(description);
			return description;
		}
	}

	public virtual HoverTip HoverTip
	{
		get
		{
			LocString description = DynamicDescription;
			if (IsMelted)
			{
				description = new LocString("gameplay_ui", "RELIC_IS_MELTED");
				description.Add("description", DynamicDescription);
			}
			else if (IsUsedUp && IsMutable)
			{
				description = new LocString("gameplay_ui", "RELIC_USED_UP");
				description.Add("description", DynamicDescription);
			}
			HoverTip result = new HoverTip(Title, description);
			result.SetCanonicalModel(CanonicalInstance);
			return result;
		}
	}

	protected virtual IEnumerable<IHoverTip> ExtraHoverTips => Array.Empty<IHoverTip>();

	public IEnumerable<IHoverTip> HoverTipsExcludingRelic => ExtraHoverTips;

	public IEnumerable<IHoverTip> HoverTips => new IHoverTip[1] { HoverTip }.Concat(ExtraHoverTips);

	public static bool IsBeforeAct3TreasureChest(IRunState runState)
	{
		int floorThreshold = runState.Players.Count > 1 ? 38 : 41;
		return runState.TotalFloor < floorThreshold;
	}

	protected override void AfterCloned()
	{
		base.AfterCloned();
		_owner = null;
		_pool = null;
	}

	protected override void DeepCloneFields()
	{
		base.DeepCloneFields();
		_dynamicVars = _dynamicVars != null ? DynamicVars.Clone(this) : null;
	}

	protected void RelicIconChanged()
	{
	}

	protected void InvokeDisplayAmountChanged()
	{
	}

	public void MarkRemovedFromState()
	{
		HasBeenRemovedFromState = true;
	}

	public void RemoveInternal()
	{
		MarkRemovedFromState();
		AfterRemoved();
	}

	public virtual mySerializableRelic SaveState()
	{
		mySerializableRelic save = new mySerializableRelic
		{
			Id = ContentId,
			Status = Status,
			HasBeenRemovedFromState = HasBeenRemovedFromState,
			FloorAddedToDeck = FloorAddedToDeck
		};
		WriteCustomState(save.State);
		return save;
	}

	public virtual mySerializableRelic ToSerializable()
	{
		return SaveState();
	}

	public virtual void LoadState(mySerializableRelic save)
	{
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		Status = save.Status;
		HasBeenRemovedFromState = save.HasBeenRemovedFromState;
		FloorAddedToDeck = save.FloorAddedToDeck;
		ReadCustomState(save.State);
	}

	public static TRelic RestoreState<TRelic>(mySerializableRelic save) where TRelic : RelicModel
	{
		TRelic relic = (TRelic)ModelDb.Relic<TRelic>().ToMutable();
		relic.LoadState(save);
		return relic;
	}

	public static RelicModel FromSerializable(mySerializableRelic save)
	{
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		RelicModel? relic = TryCreateKernelEquivalent(save.Id);
		if (relic == null)
		{
			throw new InvalidOperationException($"Unable to restore relic '{save.Id}' in kernel runtime.");
		}
		relic.LoadState(save);
		return relic;
	}

	public static RelicModel FromSerializable(MegaCrit.Sts2.Core.Saves.Runs.mySerializableRelic save)
	{
		return FromSerializable((mySerializableRelic)save);
	}

	public static RelicModel FromSerializable(MegaCrit.Sts2.Core.Saves.Runs.SerializableRelic save)
	{
		if (save is MegaCrit.Sts2.Core.Saves.Runs.mySerializableRelic extendedSave)
		{
			return FromSerializable(extendedSave);
		}
		return FromSerializable(new MegaCrit.Sts2.Core.Saves.Runs.mySerializableRelic
		{
			Id = save?.Id,
			FloorAddedToDeck = save?.FloorAddedToDeck
		});
	}

	public static RelicModel FromCore(RelicModel sourceRelic)
	{
		if (sourceRelic == null)
		{
			throw new ArgumentNullException(nameof(sourceRelic));
		}
		RelicModel relic = TryCreateKernelEquivalent(sourceRelic.ContentId) ?? new myWrappedCoreRelic(sourceRelic);
		relic.FloorAddedToDeck = sourceRelic.FloorAddedToDeck;
		relic.Status = sourceRelic.Status;
		if (sourceRelic.HasBeenRemovedFromState)
		{
			relic.MarkRemovedFromState();
		}
		return relic;
	}

	protected virtual void WriteCustomState(Dictionary<string, string> state)
	{
	}

	protected virtual void ReadCustomState(IReadOnlyDictionary<string, string> state)
	{
	}

	private static RelicModel? TryCreateKernelEquivalent(string relicId)
	{
		foreach (RelicModel canonical in ModelDb.AllRelics)
		{
			if (canonical.Id.Entry.Equals(relicId, StringComparison.OrdinalIgnoreCase))
			{
				return canonical.ToMutable();
			}
		}
		return null;
	}

	private sealed class myWrappedCoreRelic : RelicModel
	{
		private readonly RelicModel _sourceRelic;

		public override string ContentId => _sourceRelic.ContentId;

		public override RelicRarity Rarity => _sourceRelic.Rarity;

		public override bool HasUponPickupEffect => _sourceRelic.HasUponPickupEffect;

		public override bool IsUsedUp => _sourceRelic.IsUsedUp;

		public override bool IsStackable => _sourceRelic.IsStackable;

		public override bool AddsPet => _sourceRelic.AddsPet;

		public override bool SpawnsPets => _sourceRelic.SpawnsPets;

		public override int MerchantCost => _sourceRelic.MerchantCost;

		public myWrappedCoreRelic(RelicModel sourceRelic)
		{
			_sourceRelic = sourceRelic ?? throw new ArgumentNullException(nameof(sourceRelic));
		}
	}

	private sealed class myWrappedLegacyCoreRelic : RelicModel
	{
		private readonly MegaCrit.Sts2.Core.RelicModel _sourceRelic;

		public override string ContentId => _sourceRelic.Id.Entry;

		public override RelicRarity Rarity => _sourceRelic.Rarity;

		public override bool HasUponPickupEffect => _sourceRelic.HasUponPickupEffect;

		public override bool IsUsedUp => _sourceRelic.IsUsedUp;

		public override bool IsStackable => _sourceRelic.IsStackable;

		public override bool AddsPet => _sourceRelic.AddsPet;

		public override bool SpawnsPets => _sourceRelic.SpawnsPets;

		public override int MerchantCost => _sourceRelic.MerchantCost;

		public myWrappedLegacyCoreRelic(MegaCrit.Sts2.Core.RelicModel sourceRelic)
		{
			_sourceRelic = sourceRelic ?? throw new ArgumentNullException(nameof(sourceRelic));
		}
	}
	
	protected LocString SelectionScreenPrompt
	{
		get
		{
			LocString locString = new LocString("relics", base.Id.Entry + ".selectionScreenPrompt");
			DynamicVars.AddTo(locString);
			return locString;
		}
	}

	public virtual void AfterObtained()
	{
		return;
	}

	public virtual void AfterRemoved()
	{
		return;
	}

	public virtual RelicModel ToMutable()
	{
		RelicModel clone = (RelicModel)MutableClone();
		clone._canonicalInstance = IsMutable ? CanonicalInstance : this;
		return clone;
	}

	public override void BeforeCombatStart()
	{
		return;
	}

	public override void AfterCombatEnd(CombatRoom room)
	{
		return;
	}

	public override void AfterCombatVictory(CombatRoom room)
	{
		return;
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		return;
	}

	public virtual void AfterPlayerTurnStart(Player player, CombatState combatState)
	{
		return;
	}

	public virtual void BeforeSideTurnStart(CombatSide side, CombatState combatState)
	{
		return;
	}

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		return;
	}

	public virtual void BeforeTurnEndVeryEarly(Player player)
	{
		return;
	}

	public virtual void AfterTurnEnd(Player player)
	{
		return;
	}

	public virtual void AfterDamageReceived(Creature target, decimal damage, AbstractModel? source)
	{
		return;
	}

	public virtual void AfterDiedToDoom(Creature creature)
	{
		return;
	}

	public override void AfterEnergyReset(Player player)
	{
		return;
	}

	public override void AfterTakingExtraTurn(Player player)
	{
		return;
	}

	public virtual void AfterModifyingRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		return;
	}

	public override void AfterModifyingRewards()
	{
		return;
	}

	public virtual void AfterRestSiteHeal(Player player, int healedAmount)
	{
		return;
	}

	public override void AfterRestSiteHeal(Player player, bool isMimicked)
	{
		return;
	}

	public override void AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay)
	{
		return;
	}

	public virtual void AfterModifyingOrbPassiveTriggerCount(Player player, OrbModel orb, int amount)
	{
		return;
	}

	public virtual void AfterModifyingPowerAmountGiven(Creature target, PowerModel power, decimal amount, AbstractModel? source)
	{
		return;
	}

	public virtual void AfterModifyingPowerAmountReceived(Creature target, PowerModel power, decimal amount, AbstractModel? source)
	{
		return;
	}

	public virtual void AfterModifyingHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? source)
	{
		return;
	}

	public virtual void AfterModifyingHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? source)
	{
		return;
	}

	public virtual void AfterModifyingCardPlayCount(Player player, int count)
	{
		return;
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return count;
	}

	public override decimal ModifyHandDrawLate(Player player, decimal count)
	{
		return count;
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return amount;
	}

	public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 0m;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1m;
	}

	public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return 0m;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return 1m;
	}

	public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? source)
	{
		return amount;
	}

	public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? source)
	{
		return amount;
	}

	public virtual decimal ModifyPowerAmountGiven(Creature target, PowerModel power, decimal amount, AbstractModel? source)
	{
		return amount;
	}

	public virtual int ModifyRestSiteHealAmount(Player player, int currentAmount)
	{
		return currentAmount;
	}

	public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
	{
		if (creature?.Player == null)
		{
			return amount;
		}
		return ModifyRestSiteHealAmount(creature.Player, (int)amount);
	}

	public virtual int ModifyCardPlayCount(Player player, int count)
	{
		return count;
	}

	public virtual int ModifyOrbPassiveTriggerCounts(Player player, OrbModel orb, int amount)
	{
		return amount;
	}

	public virtual int ModifyMerchantPrice(int currentPrice)
	{
		return currentPrice;
	}

	public override int ModifyXValue(CardModel card, int xValue)
	{
		return xValue;
	}

	public virtual CardCreationOptions ModifyCardRewardCreationOptions(CardCreationOptions currentOptions)
	{
		return currentOptions;
	}

	public override void ModifyMerchantCardCreationResults(Player player, List<CardCreationResult> cards)
	{
	}

	public override IReadOnlySet<RoomType> ModifyUnknownMapPointRoomTypes(IReadOnlySet<RoomType> roomTypes)
	{
		return roomTypes;
	}

	public virtual ActMap ModifyGeneratedMap(ActMap map)
	{
		return map;
	}

	public virtual ActMap ModifyGeneratedMapLate(ActMap map)
	{
		return map;
	}

	public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		return false;
	}

	public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		return false;
	}

	public override bool TryModifyRestSiteHealRewards(Player player, List<Reward> rewards, bool isMimicked)
	{
		return false;
	}

	public virtual bool TryModifyRestSiteOptions(Player player, List<RestSiteOption> options)
	{
		return false;
	}

	public virtual bool TryModifyCardRewardOptions(Player player, List<CardModel> cards)
	{
		return false;
	}

	public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewards, CardCreationOptions creationOptions)
	{
		return false;
	}

	public virtual bool TryModifyCardRewardOptionsLate(Player player, List<CardModel> cards)
	{
		return false;
	}

	public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards, CardCreationOptions creationOptions)
	{
		return false;
	}

	public override void AfterModifyingCardRewardOptions()
	{
		return;
	}

	public virtual bool TryModifyCardRewardAlternatives(Player player, List<CardRewardAlternative> alternatives)
	{
		return false;
	}

	public virtual bool TryModifyCardBeingAddedToDeck(CardModel card)
	{
		return false;
	}

	public virtual bool TryModifyPowerAmountReceived(Creature target, PowerModel power, ref decimal amount, AbstractModel? source)
	{
		return false;
	}

	public virtual bool TryModifyEnergyCostInCombat(CardModel card, ref int currentCost)
	{
		return false;
	}

	public virtual bool TryModifyStarCost(CardModel card, ref int currentCost)
	{
		return false;
	}

	public virtual bool ShouldGainGold(int amount)
	{
		return true;
	}

	public virtual bool ShouldProcurePotion(Player player, PotionModel potion)
	{
		return true;
	}

	public virtual bool ShouldForcePotionReward(Player player)
	{
		return false;
	}

	public override bool ShouldForcePotionReward(Player player, RoomType roomType)
	{
		return ShouldForcePotionReward(player);
	}

	public virtual bool ShouldGenerateTreasure(IRunState runState)
	{
		return true;
	}

	public override bool ShouldGenerateTreasure(Player player)
	{
		return player == null || ShouldGenerateTreasure(player.RunState);
	}

	public override bool ShouldTakeExtraTurn(Player player)
	{
		return false;
	}

	public virtual bool IsAllowed(IRunState runState)
	{
		return true;
	}
}
