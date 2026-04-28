using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models;

public abstract class AbstractModel : IComparable<AbstractModel>
{
	public ModelId Id { get; }

	public bool IsMutable { get; private set; }

	public bool IsCanonical => !IsMutable;

	public int CategorySortingId { get; private set; }

	public int EntrySortingId { get; private set; }

	public virtual bool PreviewOutsideOfCombat => false;

	public abstract bool ShouldReceiveCombatHooks { get; }

	public event Action<AbstractModel>? ExecutionFinished;

	protected AbstractModel()
	{
		Type type = GetType();
		if (ModelDb.Contains(type))
		{
			throw new DuplicateModelException(type);
		}
		Id = ModelDb.GetId(type);
	}

	public void InitId(ModelId id)
	{
		AssertCanonical();
		CategorySortingId = ModelIdSerializationCache.GetNetIdForCategory(Id.Category);
		EntrySortingId = ModelIdSerializationCache.GetNetIdForEntry(Id.Entry);
	}

	public virtual int CompareTo(AbstractModel? other)
	{
		if (this == other)
		{
			return 0;
		}
		if (other == null)
		{
			return 1;
		}
		return Id.CompareTo(other.Id);
	}

	public void AssertMutable()
	{
		if (!IsMutable)
		{
			throw new CanonicalModelException(GetType());
		}
	}

	public void AssertCanonical()
	{
		if (IsMutable)
		{
			throw new MutableModelException(GetType());
		}
	}

	public AbstractModel ClonePreservingMutability()
	{
		if (!IsMutable)
		{
			return this;
		}
		return MutableClone();
	}

	public AbstractModel MutableClone()
	{
		AbstractModel abstractModel = (AbstractModel)MemberwiseClone();
		abstractModel.IsMutable = true;
		abstractModel.DeepCloneFields();
		abstractModel.AfterCloned();
		return abstractModel;
	}

	protected virtual void DeepCloneFields()
	{
		AssertMutable();
	}

	protected virtual void AfterCloned()
	{
		this.ExecutionFinished = null;
	}

	public void InvokeExecutionFinished()
	{
		this.ExecutionFinished?.Invoke(this);
	}

	public virtual void AfterActEntered()
	{
		return;
	}

	public virtual void AfterAddToDeckPrevented(CardModel card)
	{
		return;
	}

	public virtual void BeforeAttack(AttackCommand command)
	{
		return;
	}

	public virtual void AfterAttack(AttackCommand command)
	{
		return;
	}

	public virtual void AfterBlockCleared(Creature creature)
	{
		return;
	}

	public virtual void BeforeBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		return;
	}

	public virtual void AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		return;
	}

	public virtual void AfterBlockBroken(Creature creature)
	{
		return;
	}

	public virtual void AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
	{
		return;
	}

	public virtual void AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
	{
		return;
	}

	public virtual void AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
		return;
	}

	public virtual void AfterCardDrawnEarly(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		return;
	}

	public virtual void AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		return;
	}

	public virtual void AfterCardEnteredCombat(CardModel card)
	{
		return;
	}

	public virtual void AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
	{
		return;
	}

	public virtual void AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		return;
	}

	public virtual void BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type)
	{
		return;
	}

	public virtual void BeforeCardPlayed(CardPlay cardPlay)
	{
		return;
	}

	public virtual void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		return;
	}

	public virtual void AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return;
	}

	public virtual void AfterCardRetained(CardModel card)
	{
		return;
	}

	public virtual void BeforeCombatStart()
	{
		return;
	}

	public virtual void BeforeCombatStartLate()
	{
		return;
	}

	public virtual void AfterCombatEnd(CombatRoom room)
	{
		return;
	}

	public virtual void AfterCombatVictoryEarly(CombatRoom room)
	{
		return;
	}

	public virtual void AfterCombatVictory(CombatRoom room)
	{
		return;
	}

	public virtual void AfterCreatureAddedToCombat(Creature creature)
	{
		return;
	}

	public virtual void AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		return;
	}

	public virtual void AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		return;
	}

	public virtual void BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return;
	}

	public virtual void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return;
	}

	public virtual void AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return;
	}

	public virtual void BeforeDeath(Creature creature)
	{
		return;
	}

	public virtual void AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		return;
	}

	public virtual void AfterDiedToDoom(PlayerChoiceContext choiceContext, IReadOnlyList<Creature> creatures)
	{
		return;
	}

	public virtual void AfterEnergyReset(Player player)
	{
		return;
	}

	public virtual void AfterEnergyResetLate(Player player)
	{
		return;
	}

	public virtual void AfterEnergySpent(CardModel card, int amount)
	{
		return;
	}

	public virtual void BeforeCardRemoved(CardModel card)
	{
		return;
	}

	public virtual void BeforeFlush(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public virtual void BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public virtual void AfterGoldGained(Player player)
	{
		return;
	}

	public virtual void BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
	{
		return;
	}

	public virtual void BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
	{
		return;
	}

	public virtual void AfterHandEmptied(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public virtual void AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
	{
		return;
	}

	public virtual void AfterMapGenerated(ActMap map, int actIndex)
	{
		return;
	}

	public virtual void AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay)
	{
		return;
	}

	public virtual void AfterModifyingCardPlayCount(CardModel card)
	{
		return;
	}

	public virtual void AfterModifyingCardPlayResultPileOrPosition(CardModel card, PileType pileType, CardPilePosition position)
	{
		return;
	}

	public virtual void AfterModifyingOrbPassiveTriggerCount(OrbModel orb)
	{
		return;
	}

	public virtual void AfterModifyingCardRewardOptions()
	{
		return;
	}

	public virtual void AfterModifyingDamageAmount(CardModel? cardSource)
	{
		return;
	}

	public virtual void AfterModifyingEnergyGain()
	{
		return;
	}

	public virtual void AfterModifyingHandDraw()
	{
		return;
	}

	public virtual void AfterPreventingDraw()
	{
		return;
	}

	public virtual void AfterModifyingHpLostBeforeOsty()
	{
		return;
	}

	public virtual void AfterModifyingHpLostAfterOsty()
	{
		return;
	}

	public virtual void AfterModifyingPowerAmountReceived(PowerModel power)
	{
		return;
	}

	public virtual void AfterModifyingPowerAmountGiven(PowerModel power)
	{
		return;
	}

	public virtual void AfterModifyingRewards()
	{
		return;
	}

	public virtual void BeforeRewardsOffered(Player player, IReadOnlyList<Reward> rewards)
	{
		return;
	}

	public virtual void AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
	{
		return;
	}

	public virtual void AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
	{
		return;
	}

	public virtual void AfterOstyRevived(Creature osty)
	{
		return;
	}

	public virtual void BeforePotionUsed(PotionModel potion, Creature? target)
	{
		return;
	}

	public virtual void AfterPotionUsed(PotionModel potion, Creature? target)
	{
		return;
	}

	public virtual void AfterPotionDiscarded(PotionModel potion)
	{
		return;
	}

	public virtual void AfterPotionProcured(PotionModel potion)
	{
		return;
	}

	public virtual void BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
	{
		return;
	}

	public virtual void AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return;
	}

	public virtual void AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
	{
		return;
	}

	public virtual void AfterPreventingDeath(Creature creature)
	{
		return;
	}

	public virtual void AfterRestSiteHeal(Player player, bool isMimicked)
	{
		return;
	}

	public virtual void AfterRestSiteSmith(Player player)
	{
		return;
	}

	public virtual void AfterRewardTaken(Player player, Reward reward)
	{
		return;
	}

	public virtual void BeforeRoomEntered(AbstractRoom room)
	{
		return;
	}

	public virtual void AfterRoomEntered(AbstractRoom room)
	{
		return;
	}

	public virtual void AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		return;
	}

	public virtual void AfterStarsSpent(int amount, Player spender)
	{
		return;
	}

	public virtual void AfterStarsGained(int amount, Player gainer)
	{
		return;
	}

	public virtual void AfterForge(decimal amount, Player forger, AbstractModel? source)
	{
		return;
	}

	public virtual void AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
	{
		return;
	}

	public virtual void AfterTakingExtraTurn(Player player)
	{
		return;
	}

	public virtual void AfterTargetingBlockedVfx(Creature blocker)
	{
		return;
	}

	public virtual void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		return;
	}

	public virtual void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		return;
	}
	
	public virtual void AfterSideTurnStartLate(CombatSide side, CombatState combatState)
	{
		return;
	}

	public virtual void AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public virtual void AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public virtual void AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public virtual void BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public virtual void BeforePlayPhaseStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		return;
	}

	public virtual void BeforeTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return;
	}

	public virtual void BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return;
	}

	public virtual void BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return;
	}

	public virtual void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return;
	}

	public virtual void AfterTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return;
	}

	public virtual int ModifyAttackHitCount(AttackCommand attack, int hitCount)
	{
		return hitCount;
	}

	public virtual decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return 0m;
	}

	public virtual decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return 1m;
	}

	public virtual int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		return playCount;
	}

	public virtual (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
	{
		return (pileType, position);
	}

	public virtual int ModifyOrbPassiveTriggerCounts(OrbModel orb, int triggerCount)
	{
		return triggerCount;
	}

	public virtual CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
	{
		return options;
	}

	public virtual CardCreationOptions ModifyCardRewardCreationOptionsLate(Player player, CardCreationOptions options)
	{
		return options;
	}

	public virtual decimal ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds)
	{
		return odds;
	}

	public virtual decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 0m;
	}

	public virtual decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return decimal.MaxValue;
	}

	public virtual decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1m;
	}

	public virtual decimal ModifyEnergyGain(Player player, decimal amount)
	{
		return amount;
	}

	public virtual ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
	{
		return map;
	}

	public virtual ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
	{
		return map;
	}

	public virtual decimal ModifyHandDraw(Player player, decimal count)
	{
		return count;
	}

	public virtual decimal ModifyHandDrawLate(Player player, decimal count)
	{
		return count;
	}

	public virtual decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return amount;
	}

	public virtual decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return amount;
	}

	public virtual decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return amount;
	}

	public virtual decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return amount;
	}

	public virtual decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return amount;
	}

	public virtual IEnumerable<CardModel> ModifyMerchantCardPool(Player player, IEnumerable<CardModel> options)
	{
		return options;
	}

	public virtual CardRarity ModifyMerchantCardRarity(Player player, CardRarity rarity)
	{
		return rarity;
	}

	public virtual void ModifyMerchantCardCreationResults(Player player, List<CardCreationResult> cards)
	{
	}

	public virtual decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
	{
		return cost;
	}

	public virtual decimal ModifyOrbValue(Player player, decimal value)
	{
		return value;
	}

	public virtual decimal ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		return amount;
	}

	public virtual decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
	{
		return amount;
	}

	public virtual void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)
	{
	}

	public virtual decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)
	{
		return amount;
	}

	public virtual Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer)
	{
		return target;
	}

	public virtual EventModel ModifyNextEvent(EventModel currentEvent)
	{
		return currentEvent;
	}

	public virtual IReadOnlySet<RoomType> ModifyUnknownMapPointRoomTypes(IReadOnlySet<RoomType> roomTypes)
	{
		return roomTypes;
	}

	public virtual float ModifyOddsIncreaseForUnrolledRoomType(RoomType roomType, float oddsIncrease)
	{
		return oddsIncrease;
	}

	public virtual int ModifyXValue(CardModel card, int originalValue)
	{
		return originalValue;
	}

	public virtual bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
	{
		newCard = null;
		return false;
	}

	public virtual bool TryModifyCardBeingAddedToDeckLate(CardModel card, out CardModel? newCard)
	{
		newCard = null;
		return false;
	}

	public virtual bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
	{
		return false;
	}

	public virtual bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		return false;
	}

	public virtual bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		return false;
	}

	public virtual bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		return false;
	}

	public virtual bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		return false;
	}

	public virtual bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		return false;
	}

	public virtual bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		return false;
	}

	public virtual bool TryModifyRestSiteHealRewards(Player player, List<Reward> rewards, bool isMimicked)
	{
		return false;
	}

	public virtual bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		return false;
	}

	public virtual bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		return false;
	}

	public virtual IReadOnlyList<LocString> ModifyExtraRestSiteHealText(Player player, IReadOnlyList<LocString> currentExtraText)
	{
		return currentExtraText;
	}

	public virtual bool ShouldAddToDeck(CardModel card)
	{
		return true;
	}

	public virtual bool ShouldAfflict(CardModel card, AfflictionModel affliction)
	{
		return true;
	}

	public virtual bool ShouldAllowAncient(Player player, AncientEventModel ancient)
	{
		return true;
	}

	public virtual bool ShouldAllowHitting(Creature creature)
	{
		return true;
	}

	public virtual bool ShouldAllowTargeting(Creature target)
	{
		return true;
	}

	public virtual bool ShouldAllowSelectingMoreCardRewards(Player player, CardReward cardReward)
	{
		return false;
	}

	public virtual bool ShouldClearBlock(Creature creature)
	{
		return true;
	}

	public virtual bool ShouldDie(Creature creature)
	{
		return true;
	}

	public virtual bool ShouldDieLate(Creature creature)
	{
		return true;
	}

	public virtual bool ShouldDisableRemainingRestSiteOptions(Player player)
	{
		return true;
	}

	public virtual bool ShouldDraw(Player player, bool fromHandDraw)
	{
		return true;
	}

	public virtual bool ShouldEtherealTrigger(CardModel card)
	{
		return true;
	}

	public virtual bool ShouldFlush(Player player)
	{
		return true;
	}

	public virtual bool ShouldGainGold(decimal amount, Player player)
	{
		return true;
	}

	public virtual bool ShouldGainStars(decimal amount, Player player)
	{
		return true;
	}

	public virtual bool ShouldGenerateTreasure(Player player)
	{
		return true;
	}

	public virtual bool ShouldPayExcessEnergyCostWithStars(Player player)
	{
		return false;
	}

	public virtual bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		return true;
	}

	public virtual bool ShouldPlayerResetEnergy(Player player)
	{
		return true;
	}

	public virtual bool ShouldProceedToNextMapPoint()
	{
		return true;
	}

	public virtual bool ShouldProcurePotion(PotionModel potion, Player player)
	{
		return true;
	}

	public virtual bool ShouldPowerBeRemovedOnDeath(PowerModel power)
	{
		return true;
	}

	public virtual bool ShouldRefillMerchantEntry(MerchantEntry entry, Player player)
	{
		return false;
	}

	public virtual bool ShouldAllowMerchantCardRemoval(Player player)
	{
		return true;
	}

	public virtual bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		return true;
	}

	public virtual bool ShouldStopCombatFromEnding()
	{
		return false;
	}

	public virtual bool ShouldTakeExtraTurn(Player player)
	{
		return false;
	}

	public virtual bool ShouldForcePotionReward(Player player, RoomType roomType)
	{
		return false;
	}

	public virtual bool ShouldAllowFreeTravel()
	{
		return false;
	}

	public override string ToString()
	{
		return $"{Id} ({RuntimeHelpers.GetHashCode(this)})";
	}

	protected void NeverEverCallThisOutsideOfTests_SetIsMutable(bool isMutable)
	{
		if (TestMode.IsOff)
		{
			throw new InvalidOperationException("You monster!");
		}
		IsMutable = isMutable;
	}

	protected static void RunSynchronously(Task task)
	{
		task.GetAwaiter().GetResult();
	}

	protected static T RunSynchronously<T>(Task<T> task)
	{
		return task.GetAwaiter().GetResult();
	}

	protected static T? RunSynchronously<T>(T? value)
	{
		return value;
	}
}
