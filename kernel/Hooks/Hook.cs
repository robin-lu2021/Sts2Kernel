using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Hooks;

public static class Hook
{
	public static void AfterActEntered(IRunState runState)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.AfterActEntered();
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeAttack(CombatState combatState, AttackCommand command)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.BeforeAttack(command);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterAttack(CombatState combatState, AttackCommand command)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterAttack(command);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterBlockBroken(CombatState combatState, Creature creature)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterBlockBroken(creature);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterBlockCleared(CombatState combatState, Creature creature)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterBlockCleared(creature);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeBlockGained(CombatState combatState, Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.BeforeBlockGained(creature, amount, props, cardSource);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterBlockGained(CombatState combatState, Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterBlockGained(creature, amount, props, cardSource);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeCardAutoPlayed(CombatState combatState, CardModel card, Creature? target, AutoPlayType type)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.BeforeCardAutoPlayed(card, target, type);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterCardChangedPiles(IRunState runState, CombatState? combatState, CardModel card, PileType oldPile, AbstractModel? source)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.AfterCardChangedPiles(card, oldPile, source);
			model.InvokeExecutionFinished();
		}
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.AfterCardChangedPilesLate(card, oldPile, source);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterCardDiscarded(CombatState combatState, PlayerChoiceContext choiceContext, CardModel card)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterCardDiscarded(choiceContext, card);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
	}

	public static void AfterCardDrawn(CombatState combatState, PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterCardDrawnEarly(choiceContext, card, fromHandDraw);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterCardDrawn(choiceContext, card, fromHandDraw);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
	}

	public static void AfterCardEnteredCombat(CombatState combatState, CardModel card)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterCardEnteredCombat(card);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterCardExhausted(CombatState combatState, PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterCardExhausted(choiceContext, card, causedByEthereal);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
	}

	public static void AfterCardGeneratedForCombat(CombatState combatState, CardModel card, bool addedByPlayer)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterCardGeneratedForCombat(card, addedByPlayer);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeCardPlayed(CombatState combatState, CardPlay cardPlay)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.BeforeCardPlayed(cardPlay);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterCardPlayed(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterCardPlayed(choiceContext, cardPlay);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterCardPlayedLate(choiceContext, cardPlay);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
	}

	public static void BeforeCardRemoved(IRunState runState, CardModel card)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.BeforeCardRemoved(card);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterCardRetained(CombatState combatState, CardModel card)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterCardRetained(card);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeCombatStart(IRunState runState, CombatState? combatState)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.BeforeCombatStart();
			model.InvokeExecutionFinished();
		}
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.BeforeCombatStartLate();
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterCombatEnd(IRunState runState, CombatState? combatState, CombatRoom room)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.AfterCombatEnd(room);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterCombatVictory(IRunState runState, CombatState? combatState, CombatRoom room)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.AfterCombatVictoryEarly(room);
			model.InvokeExecutionFinished();
		}
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.AfterCombatVictory(room);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterCreatureAddedToCombat(CombatState combatState, Creature creature)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterCreatureAddedToCombat(creature);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterCurrentHpChanged(IRunState runState, CombatState? combatState, Creature creature, decimal delta)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.AfterCurrentHpChanged(creature, delta);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterDamageGiven(PlayerChoiceContext choiceContext, CombatState combatState, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterDamageGiven(choiceContext, dealer, results, props, target, cardSource);
			choiceContext.PopModel(model);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, CombatState? combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			choiceContext.PushModel(model);
			model.BeforeDamageReceived(choiceContext, target, amount, props, dealer, cardSource);
			choiceContext.PopModel(model);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterDamageReceived(PlayerChoiceContext choiceContext, IRunState runState, CombatState? combatState, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			choiceContext.PushModel(model);
			model.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
			choiceContext.PopModel(model);
			model.InvokeExecutionFinished();
		}
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			choiceContext.PushModel(model);
			model.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource);
			choiceContext.PopModel(model);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeDeath(IRunState runState, CombatState? combatState, Creature creature)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.BeforeDeath(creature);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterDeath(IRunState runState, CombatState? combatState, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		ulong? netId = LocalContext.NetId;
		if (!netId.HasValue)
		{
			PhrogDebug.LogInfo($"Hook.AfterDeath skipped because LocalContext.NetId is null: creature={creature.ModelId.Entry}, combatId={creature.CombatId?.ToString() ?? "null"}, powers={string.Join(",", creature.Powers.Select(p => p.Id.Entry + ":" + p.Amount))}");
			return;
		}
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			HookPlayerChoiceContext hookPlayerChoiceContext = new HookPlayerChoiceContext(model, netId.Value, creature.CombatState, GameActionType.Combat);
			model.AfterDeath(hookPlayerChoiceContext, creature, wasRemovalPrevented, deathAnimLength);
			Task task = Task.CompletedTask;
			hookPlayerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterGoldGained(IRunState runState, Player player)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.AfterGoldGained(player);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterDiedToDoom(CombatState combatState, IReadOnlyList<Creature> creatures)
	{
		ulong? netId = LocalContext.NetId;
		if (!netId.HasValue)
		{
			return;
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			HookPlayerChoiceContext hookPlayerChoiceContext = new HookPlayerChoiceContext(model, netId.Value, combatState, GameActionType.Combat);
			model.AfterDiedToDoom(hookPlayerChoiceContext, creatures);
			Task task = Task.CompletedTask;
			hookPlayerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterEnergyReset(CombatState combatState, Player player)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterEnergyReset(player);
			model.InvokeExecutionFinished();
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterEnergyResetLate(player);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterEnergySpent(CombatState combatState, CardModel card, int amount)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterEnergySpent(card, amount);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeFlush(CombatState combatState, Player player)
	{
		ulong? netId = LocalContext.NetId;
		if (!netId.HasValue)
		{
			return;
		}
		List<Task> tasksToAwait = new List<Task>();
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			HookPlayerChoiceContext playerChoiceContext = new HookPlayerChoiceContext(item, netId.Value, player.Creature.CombatState, GameActionType.Combat);
			item.BeforeFlush(playerChoiceContext, player);
			Task task = Task.CompletedTask;
			if (!(playerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task)))
			{
				tasksToAwait.Add(playerChoiceContext.GameAction.CompletionTask);
			}
		}
		foreach (AbstractModel item2 in combatState.IterateHookListeners())
		{
			HookPlayerChoiceContext playerChoiceContext = new HookPlayerChoiceContext(item2, netId.Value, player.Creature.CombatState, GameActionType.Combat);
			item2.BeforeFlushLate(playerChoiceContext, player);
			Task task2 = Task.CompletedTask;
			if (!(playerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task2)))
			{
				tasksToAwait.Add(playerChoiceContext.GameAction.CompletionTask);
			}
		}
		Task.WhenAll(tasksToAwait);
	}

	public static void AfterForge(CombatState combatState, decimal amount, Player forger, AbstractModel? source)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterForge(amount, forger, source);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeHandDraw(CombatState combatState, Player player, PlayerChoiceContext playerChoiceContext)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			playerChoiceContext.PushModel(model);
			model.BeforeHandDraw(player, playerChoiceContext, combatState);
			model.InvokeExecutionFinished();
			playerChoiceContext.PopModel(model);
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			playerChoiceContext.PushModel(model);
			model.BeforeHandDrawLate(player, playerChoiceContext, combatState);
			model.InvokeExecutionFinished();
			playerChoiceContext.PopModel(model);
		}
	}

	public static void AfterHandEmptied(CombatState combatState, PlayerChoiceContext choiceContext, Player player)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterHandEmptied(choiceContext, player);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
	}

	public static void AfterItemPurchased(IRunState runState, Player player, MerchantEntry itemPurchased, int goldSpent)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.AfterItemPurchased(player, itemPurchased, goldSpent);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterMapGenerated(IRunState runState, ActMap map, int actIndex)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.AfterMapGenerated(map, actIndex);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterModifyingBlockAmount(CombatState combatState, decimal modifiedBlock, CardModel? cardSource, CardPlay? cardPlay, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in combatState.IterateHookListeners())
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingBlockAmount(modifiedBlock, cardSource, cardPlay);
				modifier.InvokeExecutionFinished();
			}
		}
	}

	public static void AfterModifyingCardPlayCount(CombatState combatState, CardModel card, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in combatState.IterateHookListeners())
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingCardPlayCount(card);
				modifier.InvokeExecutionFinished();
			}
		}
	}

	public static Task AfterModifyingCardRewardOptions(IRunState runState, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in runState.IterateHookListeners(null))
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingCardRewardOptions();
				modifier.InvokeExecutionFinished();
			}
		}
		return Task.CompletedTask;
	}

	public static void AfterModifyingDamageAmount(IRunState runState, CombatState? combatState, CardModel? cardSource, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in runState.IterateHookListeners(combatState))
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingDamageAmount(cardSource);
				modifier.InvokeExecutionFinished();
			}
		}
	}

	public static void AfterModifyingEnergyGain(CombatState combatState, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in combatState.IterateHookListeners())
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingEnergyGain();
				modifier.InvokeExecutionFinished();
			}
		}
	}

	public static void AfterModifyingHandDraw(CombatState combatState, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in combatState.IterateHookListeners())
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingHandDraw();
				modifier.InvokeExecutionFinished();
			}
		}
	}

	public static void AfterModifyingHpLostBeforeOsty(IRunState runState, CombatState? combatState, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in runState.IterateHookListeners(combatState))
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingHpLostBeforeOsty();
				modifier.InvokeExecutionFinished();
			}
		}
	}

	public static void AfterModifyingHpLostAfterOsty(IRunState runState, CombatState? combatState, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in runState.IterateHookListeners(combatState))
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingHpLostAfterOsty();
				modifier.InvokeExecutionFinished();
			}
		}
	}

	public static Task AfterModifyingOrbPassiveTriggerCount(CombatState combatState, OrbModel orb, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in combatState.IterateHookListeners())
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingOrbPassiveTriggerCount(orb);
				modifier.InvokeExecutionFinished();
			}
		}
		return Task.CompletedTask;
	}

	public static void AfterModifyingPowerAmountGiven(CombatState combatState, IEnumerable<AbstractModel> modifiers, PowerModel modifiedPower)
	{
		foreach (AbstractModel modifier in combatState.IterateHookListeners())
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingPowerAmountGiven(modifiedPower);
				modifier.InvokeExecutionFinished();
			}
		}
	}

	public static void AfterModifyingPowerAmountReceived(CombatState combatState, IEnumerable<AbstractModel> modifiers, PowerModel modifiedPower)
	{
		foreach (AbstractModel modifier in combatState.IterateHookListeners())
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingPowerAmountReceived(modifiedPower);
				modifier.InvokeExecutionFinished();
			}
		}
	}

	public static Task AfterModifyingRewards(IRunState runState, IEnumerable<AbstractModel> modifiers)
	{
		foreach (AbstractModel modifier in runState.IterateHookListeners(null))
		{
			if (modifiers.Contains(modifier))
			{
				modifier.AfterModifyingRewards();
				modifier.InvokeExecutionFinished();
			}
		}
		return Task.CompletedTask;
	}

	public static void AfterOrbChanneled(CombatState combatState, PlayerChoiceContext choiceContext, Player player, OrbModel orb)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterOrbChanneled(choiceContext, player, orb);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
	}

	public static void AfterOrbEvoked(PlayerChoiceContext choiceContext, CombatState combatState, OrbModel orb, IEnumerable<Creature> targets)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterOrbEvoked(choiceContext, orb, targets);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterOstyRevived(CombatState combatState, Creature osty)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterOstyRevived(osty);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterPlayerTurnStart(CombatState combatState, PlayerChoiceContext choiceContext, Player player)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterPlayerTurnStartEarly(choiceContext, player);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterPlayerTurnStart(choiceContext, player);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterPlayerTurnStartLate(choiceContext, player);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
	}
	
	public static Task BeforePlayPhaseStart(HookPlayerChoiceContext playerChoiceContext, Task setupPlayerTurnTask, CombatState combatState, Player player)
	{
		if (!LocalContext.NetId.HasValue)
		{
			return Task.CompletedTask;
		}
		setupPlayerTurnTask.GetAwaiter().GetResult();
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			playerChoiceContext.PushModel(model);
			model.BeforePlayPhaseStart(playerChoiceContext, player);
			model.InvokeExecutionFinished();
			playerChoiceContext.PopModel(model);
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			playerChoiceContext.PushModel(model);
			model.BeforePlayPhaseStartLate(playerChoiceContext, player);
			model.InvokeExecutionFinished();
			playerChoiceContext.PopModel(model);
		}
		return Task.CompletedTask;
	}

	public static void AfterPotionDiscarded(IRunState runState, CombatState? combatState, PotionModel potion)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.AfterPotionDiscarded(potion);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterPotionProcured(IRunState runState, CombatState? combatState, PotionModel potion)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.AfterPotionProcured(potion);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforePotionUsed(IRunState runState, CombatState? combatState, PotionModel potion, Creature? target)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.BeforePotionUsed(potion, target);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterPotionUsed(IRunState runState, CombatState? combatState, PotionModel potion, Creature? target)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(combatState))
		{
			model.AfterPotionUsed(potion, target);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforePowerAmountChanged(CombatState combatState, PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
	{
		foreach (AbstractModel modifier in combatState.IterateHookListeners())
		{
			modifier.BeforePowerAmountChanged(power, amount, target, applier, cardSource);
			modifier.InvokeExecutionFinished();
		}
	}

	public static void AfterPowerAmountChanged(CombatState combatState, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterPowerAmountChanged(power, amount, applier, cardSource);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterPreventingBlockClear(CombatState combatState, AbstractModel preventer, Creature creature)
	{
		if (combatState.IterateHookListeners().Contains(preventer))
		{
			preventer.AfterPreventingBlockClear(preventer, creature);
			preventer.InvokeExecutionFinished();
		}
	}

	public static void AfterPreventingDeath(IRunState runState, CombatState? combatState, AbstractModel preventer, Creature creature)
	{
		if (runState.IterateHookListeners(combatState).Contains(preventer))
		{
			preventer.AfterPreventingDeath(creature);
			preventer.InvokeExecutionFinished();
		}
	}

	public static void AfterPreventingDraw(CombatState combatState, AbstractModel modifier)
	{
		if (combatState.IterateHookListeners().Contains(modifier))
		{
			modifier.AfterPreventingDraw();
			modifier.InvokeExecutionFinished();
		}
	}

	public static void AfterRestSiteHeal(IRunState runState, Player player, bool isMimicked)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.AfterRestSiteHeal(player, isMimicked);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterRestSiteSmith(IRunState runState, Player player)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.AfterRestSiteSmith(player);
			model.InvokeExecutionFinished();
		}
	}

	public static Task BeforeRewardsOffered(IRunState runState, Player player, IReadOnlyList<Reward> rewards)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.BeforeRewardsOffered(player, rewards);
			model.InvokeExecutionFinished();
		}
		return Task.CompletedTask;
	}

	public static Task AfterRewardTaken(IRunState runState, Player player, Reward reward)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.AfterRewardTaken(player, reward);
			model.InvokeExecutionFinished();
		}
		return Task.CompletedTask;
	}

	public static void BeforeRoomEntered(IRunState runState, AbstractRoom room)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.BeforeRoomEntered(room);
			model.InvokeExecutionFinished();
		}
	}

	public static Task AfterRoomEntered(IRunState runState, AbstractRoom room)
	{
		foreach (AbstractModel model in runState.IterateHookListeners(null))
		{
			model.AfterRoomEntered(room);
			model.InvokeExecutionFinished();
		}
		return Task.CompletedTask;
	}

	public static void AfterShuffle(CombatState combatState, PlayerChoiceContext choiceContext, Player shuffler)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterShuffle(choiceContext, shuffler);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
	}

	public static void BeforeSideTurnStart(CombatState combatState, CombatSide side)
	{
		ulong? netId = LocalContext.NetId;
		if (!netId.HasValue)
		{
			return;
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			HookPlayerChoiceContext hookPlayerChoiceContext = new HookPlayerChoiceContext(model, netId.Value, combatState, GameActionType.Combat);
			model.BeforeSideTurnStart(hookPlayerChoiceContext, side, combatState);
			hookPlayerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(Task.CompletedTask);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterSideTurnStart(CombatState combatState, CombatSide side)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterSideTurnStart(side, combatState);
			model.InvokeExecutionFinished();
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterSideTurnStartLate(side, combatState);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterStarsGained(CombatState combatState, int amount, Player gainer)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterStarsGained(amount, gainer);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterStarsSpent(CombatState combatState, int amount, Player spender)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterStarsSpent(amount, spender);
			model.InvokeExecutionFinished();
		}
	}

	public static void AfterSummon(CombatState combatState, PlayerChoiceContext choiceContext, Player summoner, decimal amount)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			choiceContext.PushModel(model);
			model.AfterSummon(choiceContext, summoner, amount);
			model.InvokeExecutionFinished();
			choiceContext.PopModel(model);
		}
	}

	public static void AfterTakingExtraTurn(CombatState combatState, Player player)
	{
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			model.AfterTakingExtraTurn(player);
			model.InvokeExecutionFinished();
		}
	}

	public static void BeforeTurnEnd(CombatState combatState, CombatSide side)
	{
		ulong? netId = LocalContext.NetId;
		if (!netId.HasValue)
		{
			return;
		}
		List<Task> tasksToAwait = new List<Task>();
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			HookPlayerChoiceContext playerChoiceContext = new HookPlayerChoiceContext(item, netId.Value, combatState, GameActionType.Combat);
			item.BeforeTurnEndVeryEarly(playerChoiceContext, side);
			Task task = Task.CompletedTask;
			if (!(playerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task)))
			{
				tasksToAwait.Add(playerChoiceContext.GameAction.CompletionTask);
			}
		}
		foreach (AbstractModel item2 in combatState.IterateHookListeners())
		{
			HookPlayerChoiceContext playerChoiceContext = new HookPlayerChoiceContext(item2, netId.Value, combatState, GameActionType.Combat);
			item2.BeforeTurnEndEarly(playerChoiceContext, side);
			Task task2 = Task.CompletedTask;
			if (!(playerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task2)))
			{
				tasksToAwait.Add(playerChoiceContext.GameAction.CompletionTask);
			}
		}
		foreach (AbstractModel item3 in combatState.IterateHookListeners())
		{
			HookPlayerChoiceContext playerChoiceContext = new HookPlayerChoiceContext(item3, netId.Value, combatState, GameActionType.Combat);
			item3.BeforeTurnEnd(playerChoiceContext, side);
			Task task3 = Task.CompletedTask;
			if (!(playerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task3)))
			{
				tasksToAwait.Add(playerChoiceContext.GameAction.CompletionTask);
			}
		}
		Task.WhenAll(tasksToAwait);
	}

	public static void AfterTurnEnd(CombatState combatState, CombatSide side)
	{
		ulong? netId = LocalContext.NetId;
		if (!netId.HasValue)
		{
			return;
		}
		List<Task> tasksToAwait = new List<Task>();
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			HookPlayerChoiceContext playerChoiceContext = new HookPlayerChoiceContext(item, netId.Value, combatState, GameActionType.Combat);
			item.AfterTurnEnd(playerChoiceContext, side);
			Task task = Task.CompletedTask;
			if (!(playerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task)))
			{
				tasksToAwait.Add(playerChoiceContext.GameAction.CompletionTask);
			}
		}
		Task.WhenAll(tasksToAwait);
		tasksToAwait.Clear();
		foreach (AbstractModel item2 in combatState.IterateHookListeners())
		{
			HookPlayerChoiceContext playerChoiceContext = new HookPlayerChoiceContext(item2, netId.Value, combatState, GameActionType.Combat);
			item2.AfterTurnEndLate(playerChoiceContext, side);
			Task task2 = Task.CompletedTask;
			if (!(playerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task2)))
			{
				tasksToAwait.Add(playerChoiceContext.GameAction.CompletionTask);
			}
		}
		Task.WhenAll(tasksToAwait);
	}

	public static decimal ModifyAttackHitCount(CombatState combatState, AttackCommand attackCommand, int originalHitCount)
	{
		int num = originalHitCount;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			num = item.ModifyAttackHitCount(attackCommand, num);
		}
		return num;
	}

	public static decimal ModifyBlock(CombatState combatState, Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay, out IEnumerable<AbstractModel> modifiers)
	{
		List<AbstractModel> list = new List<AbstractModel>();
		decimal num = block;
		if (cardSource != null && cardSource.Enchantment != null)
		{
			EnchantmentModel enchantment = cardSource.Enchantment;
			num += enchantment.EnchantBlockAdditive(num, props);
			num *= enchantment.EnchantBlockMultiplicative(num, props);
		}
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			decimal num2 = item.ModifyBlockAdditive(target, num, props, cardSource, cardPlay);
			num += num2;
			if (num2 != 0m)
			{
				list.Add(item);
			}
		}
		foreach (AbstractModel item2 in combatState.IterateHookListeners())
		{
			decimal num3 = item2.ModifyBlockMultiplicative(target, num, props, cardSource, cardPlay);
			num *= num3;
			if (num3 != 1m)
			{
				list.Add(item2);
			}
		}
		modifiers = list;
		return Math.Max(0m, num);
	}

	public static CardModel ModifyCardBeingAddedToDeck(IRunState runState, CardModel card, out List<AbstractModel> modifyingModels)
	{
		modifyingModels = new List<AbstractModel>();
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (item.TryModifyCardBeingAddedToDeck(card, out CardModel newCard) && newCard != null)
			{
				modifyingModels.Add(item);
				card = newCard;
			}
			item.InvokeExecutionFinished();
		}
		foreach (AbstractModel item2 in runState.IterateHookListeners(null))
		{
			if (item2.TryModifyCardBeingAddedToDeckLate(card, out CardModel newCard2) && newCard2 != null)
			{
				modifyingModels.Add(item2);
				card = newCard2;
			}
			item2.InvokeExecutionFinished();
		}
		return card;
	}

	public static int ModifyCardPlayCount(CombatState combatState, CardModel card, int playCount, Creature? target, out List<AbstractModel> modifyingModels)
	{
		modifyingModels = new List<AbstractModel>();
		int num = playCount;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			int num2 = num;
			num = item.ModifyCardPlayCount(card, target, num);
			if (num != num2)
			{
				modifyingModels.Add(item);
			}
		}
		return num;
	}

	public static (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CombatState combatState, CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position, out IEnumerable<AbstractModel> modifiers)
	{
		PileType pileType2 = pileType;
		CardPilePosition cardPilePosition = position;
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			PileType pileType3 = pileType2;
			CardPilePosition cardPilePosition2 = cardPilePosition;
			(pileType2, cardPilePosition) = item.ModifyCardPlayResultPileTypeAndPosition(card, isAutoPlay, resources, pileType2, cardPilePosition);
			if (pileType3 != pileType2 || cardPilePosition2 != cardPilePosition)
			{
				list.Add(item);
			}
		}
		modifiers = list;
		return (pileType2, cardPilePosition);
	}

	public static IEnumerable<AbstractModel> ModifyCardRewardAlternatives(IRunState runState, Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
	{
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (item.TryModifyCardRewardAlternatives(player, cardReward, alternatives))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static CardCreationOptions ModifyCardRewardCreationOptions(IRunState runState, Player player, CardCreationOptions options)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			options = item.ModifyCardRewardCreationOptions(player, options);
		}
		foreach (AbstractModel item2 in runState.IterateHookListeners(null))
		{
			options = item2.ModifyCardRewardCreationOptionsLate(player, options);
		}
		return options;
	}

	public static bool TryModifyCardRewardOptions(IRunState runState, Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions, out List<AbstractModel> modifiers)
	{
		bool flag = false;
		modifiers = new List<AbstractModel>();
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			bool flag2 = item.TryModifyCardRewardOptions(player, cardRewardOptions, creationOptions);
			flag = flag || flag2;
			modifiers.Add(item);
		}
		foreach (AbstractModel item2 in runState.IterateHookListeners(null))
		{
			bool flag3 = item2.TryModifyCardRewardOptionsLate(player, cardRewardOptions, creationOptions);
			flag = flag || flag3;
			modifiers.Add(item2);
		}
		return flag;
	}

	public static decimal ModifyCardRewardUpgradeOdds(IRunState runState, Player player, CardModel card, decimal originalOdds)
	{
		decimal num = originalOdds;
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			num = item.ModifyCardRewardUpgradeOdds(player, card, num);
		}
		return num;
	}

	public static decimal ModifyDamage(IRunState runState, CombatState? combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, ModifyDamageHookType modifyDamageHookType, CardPreviewMode previewMode, out IEnumerable<AbstractModel> modifiers)
	{
		List<AbstractModel> modifiers2 = new List<AbstractModel>();
		decimal num = damage;
		if (cardSource != null && cardSource.Enchantment != null)
		{
			if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Additive))
			{
				num += cardSource.Enchantment.EnchantDamageAdditive(num, props);
			}
			if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative))
			{
				num *= cardSource.Enchantment.EnchantDamageMultiplicative(num, props);
			}
		}
		bool flag = target == null && previewMode == CardPreviewMode.MultiCreatureTargeting;
		bool flag2 = flag;
		bool flag3;
		if (flag2)
		{
			if (cardSource != null)
			{
				TargetType targetType = cardSource.TargetType;
				if ((uint)(targetType - 3) <= 1u)
				{
					CardPile pile = cardSource.Pile;
					if (pile != null)
					{
						PileType type = pile.Type;
						if (type == PileType.Hand || type == PileType.Play)
						{
							flag3 = true;
							goto IL_00b3;
						}
					}
				}
			}
			flag3 = false;
			goto IL_00b3;
		}
		goto IL_00b7;
		IL_00b7:
		bool flag4 = flag2;
		bool flag5 = false;
		if (flag4)
		{
			bool flag6 = true;
			decimal? num2 = null;
			foreach (Creature item in combatState?.HittableEnemies ?? Array.Empty<Creature>())
			{
				List<AbstractModel> modifiers3;
				decimal num3 = ModifyDamageInternal(runState, combatState, item, dealer, num, props, cardSource, modifyDamageHookType, out modifiers3);
				if (!num2.HasValue)
				{
					num2 = num3;
				}
				else if ((int)num3 != (int)num2.Value)
				{
					flag6 = false;
					break;
				}
				modifiers2.AddRange(modifiers3);
			}
			if (num2.HasValue && flag6)
			{
				flag5 = true;
				num = num2.Value;
				modifiers2 = modifiers2.Distinct().ToList();
			}
			else
			{
				modifiers2.Clear();
			}
		}
		if (!flag4 || !flag5)
		{
			num = ModifyDamageInternal(runState, combatState, target, dealer, num, props, cardSource, modifyDamageHookType, out modifiers2);
		}
		modifiers = modifiers2;
		return Math.Max(0m, num);
		IL_00b3:
		flag2 = flag3;
		goto IL_00b7;
	}

	public static decimal ModifyEnergyCostInCombat(CombatState combatState, CardModel card, decimal originalCost)
	{
		if (originalCost < 0m)
		{
			return originalCost;
		}
		decimal modifiedCost = originalCost;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			item.TryModifyEnergyCostInCombat(card, modifiedCost, out modifiedCost);
		}
		return modifiedCost;
	}

	public static decimal ModifyEnergyGain(CombatState combatState, Player player, decimal originalAmount, out IEnumerable<AbstractModel> modifiers)
	{
		decimal num = originalAmount;
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			decimal num2 = num;
			num = item.ModifyEnergyGain(player, num);
			if ((int)num2 != (int)num)
			{
				list.Add(item);
			}
		}
		modifiers = list;
		return num;
	}

	public static IReadOnlyList<LocString> ModifyExtraRestSiteHealText(IRunState runState, Player player, IReadOnlyList<LocString> extraText)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			extraText = item.ModifyExtraRestSiteHealText(player, extraText);
		}
		return extraText;
	}

	public static ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			map = item.ModifyGeneratedMap(runState, map, actIndex);
			item.InvokeExecutionFinished();
		}
		return ModifyGeneratedMapLate(runState, map, actIndex);
	}

	public static ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			map = item.ModifyGeneratedMapLate(runState, map, actIndex);
			item.InvokeExecutionFinished();
		}
		return map;
	}

	public static decimal ModifyHandDraw(CombatState combatState, Player player, decimal originalCardCount, out IEnumerable<AbstractModel> modifiers)
	{
		decimal num = originalCardCount;
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			decimal num2 = num;
			num = item.ModifyHandDraw(player, num);
			if ((int)num2 != (int)num)
			{
				list.Add(item);
			}
		}
		foreach (AbstractModel item2 in combatState.IterateHookListeners())
		{
			decimal num3 = num;
			num = item2.ModifyHandDrawLate(player, num);
			if ((int)num3 != (int)num)
			{
				list.Add(item2);
			}
		}
		modifiers = list;
		return num;
	}

	public static decimal ModifyHpLostBeforeOsty(IRunState runState, CombatState? combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers)
	{
		decimal num = amount;
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in runState.IterateHookListeners(combatState))
		{
			decimal d = num;
			num = item.ModifyHpLostBeforeOsty(target, num, props, dealer, cardSource);
			if (decimal.Truncate(d) != decimal.Truncate(num))
			{
				list.Add(item);
			}
		}
		foreach (AbstractModel item2 in runState.IterateHookListeners(combatState))
		{
			decimal d2 = num;
			num = item2.ModifyHpLostBeforeOstyLate(target, num, props, dealer, cardSource);
			if (decimal.Truncate(d2) != decimal.Truncate(num))
			{
				list.Add(item2);
			}
		}
		modifiers = list;
		return num;
	}

	public static decimal ModifyHpLostAfterOsty(IRunState runState, CombatState? combatState, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers)
	{
		decimal num = amount;
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in runState.IterateHookListeners(combatState))
		{
			decimal d = num;
			num = item.ModifyHpLostAfterOsty(target, num, props, dealer, cardSource);
			if (decimal.Truncate(d) != decimal.Truncate(num))
			{
				list.Add(item);
			}
		}
		foreach (AbstractModel item2 in runState.IterateHookListeners(combatState))
		{
			decimal d2 = num;
			num = item2.ModifyHpLostAfterOstyLate(target, num, props, dealer, cardSource);
			if (decimal.Truncate(d2) != decimal.Truncate(num))
			{
				list.Add(item2);
			}
		}
		modifiers = list;
		return num;
	}

	public static decimal ModifyMaxEnergy(CombatState combatState, Player player, decimal amount)
	{
		decimal num = amount;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			num = item.ModifyMaxEnergy(player, num);
		}
		return num;
	}

	public static void ModifyMerchantCardCreationResults(IRunState runState, Player player, List<CardCreationResult> cards)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			item.ModifyMerchantCardCreationResults(player, cards);
		}
	}

	public static IEnumerable<CardModel> ModifyMerchantCardPool(IRunState runState, Player player, IEnumerable<CardModel> options)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			options = item.ModifyMerchantCardPool(player, options);
		}
		return options;
	}

	public static CardRarity ModifyMerchantCardRarity(IRunState runState, Player player, CardRarity rarity)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			rarity = item.ModifyMerchantCardRarity(player, rarity);
		}
		return rarity;
	}

	public static decimal ModifyMerchantPrice(IRunState runState, Player player, MerchantEntry entry, decimal result)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			result = item.ModifyMerchantPrice(player, entry, result);
		}
		return result;
	}

	public static EventModel ModifyNextEvent(IRunState runState, EventModel currentEvent)
	{
		EventModel eventModel = currentEvent;
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (item.ModifyNextEvent(eventModel) is EventModel modifiedEvent)
			{
				eventModel = modifiedEvent;
			}
		}
		return eventModel;
	}

	public static float ModifyOddsIncreaseForUnrolledRoomType(IRunState runState, RoomType roomType, float oddsIncrease)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			oddsIncrease = item.ModifyOddsIncreaseForUnrolledRoomType(roomType, oddsIncrease);
		}
		return oddsIncrease;
	}

	public static int ModifyOrbPassiveTriggerCount(CombatState combatState, OrbModel orb, int triggerCount, out List<AbstractModel> modifyingModels)
	{
		modifyingModels = new List<AbstractModel>();
		int num = triggerCount;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			int num2 = num;
			num = item.ModifyOrbPassiveTriggerCounts(orb, num);
			if (num != num2)
			{
				modifyingModels.Add(item);
			}
		}
		return num;
	}

	public static decimal ModifyOrbValue(CombatState combatState, Player player, decimal amount)
	{
		decimal num = amount;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			num = item.ModifyOrbValue(player, num);
		}
		return num;
	}

	public static decimal ModifyPowerAmountGiven(CombatState combatState, PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource, out IEnumerable<AbstractModel> modifiers)
	{
		decimal num = amount;
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			decimal num2 = item.ModifyPowerAmountGiven(power, giver, num, target, cardSource);
			if (decimal.Truncate(num2) != decimal.Truncate(num))
			{
				num = num2;
				list.Add(item);
			}
		}
		modifiers = list;
		return num;
	}

	public static decimal ModifyPowerAmountReceived(CombatState combatState, PowerModel canonicalPower, Creature target, decimal amount, Creature? giver, out IEnumerable<AbstractModel> modifiers)
	{
		decimal num = amount;
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (item.TryModifyPowerAmountReceived(canonicalPower, target, num, giver, out var modifiedAmount))
			{
				num = modifiedAmount;
				list.Add(item);
			}
		}
		modifiers = list;
		return num;
	}

	public static decimal ModifyRestSiteHealAmount(IRunState runState, Creature creature, decimal amount)
	{
		decimal num = amount;
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			num = item.ModifyRestSiteHealAmount(creature, num);
		}
		return num;
	}

	public static IEnumerable<AbstractModel> ModifyRestSiteOptions(IRunState runState, Player player, ICollection<RestSiteOption> options)
	{
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (item.TryModifyRestSiteOptions(player, options))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static IEnumerable<AbstractModel> ModifyRestSiteHealRewards(IRunState runState, Player player, List<Reward> rewards, bool isMimicked)
	{
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (item.TryModifyRestSiteHealRewards(player, rewards, isMimicked))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static IEnumerable<AbstractModel> ModifyRewards(IRunState runState, Player player, List<Reward> rewards, AbstractRoom? room)
	{
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (item.TryModifyRewards(player, rewards, room))
			{
				list.Add(item);
			}
		}
		foreach (AbstractModel item2 in runState.IterateHookListeners(null))
		{
			if (item2.TryModifyRewardsLate(player, rewards, room))
			{
				list.Add(item2);
			}
		}
		return list;
	}

	public static void ModifyShuffleOrder(CombatState combatState, Player player, List<CardModel> cards, bool isInitialShuffle)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			item.ModifyShuffleOrder(player, cards, isInitialShuffle);
		}
	}

	public static decimal ModifyStarCost(CombatState combatState, CardModel card, decimal originalCost)
	{
		if (originalCost < 0m)
		{
			return originalCost;
		}
		decimal modifiedCost = originalCost;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			item.TryModifyStarCost(card, modifiedCost, out modifiedCost);
		}
		return modifiedCost;
	}

	public static decimal ModifySummonAmount(CombatState combatState, Player summoner, decimal amount, AbstractModel? source)
	{
		decimal num = amount;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			num = item.ModifySummonAmount(summoner, num, source);
		}
		return num;
	}

	public static Creature ModifyUnblockedDamageTarget(CombatState combatState, Creature originalTarget, decimal amount, ValueProp props, Creature? dealer)
	{
		Creature creature = originalTarget;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			creature = item.ModifyUnblockedDamageTarget(creature, amount, props, dealer);
		}
		return creature;
	}

	public static IReadOnlySet<RoomType> ModifyUnknownMapPointRoomTypes(IRunState runState, IReadOnlySet<RoomType> roomTypes)
	{
		IReadOnlySet<RoomType> readOnlySet = new HashSet<RoomType>(roomTypes);
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			readOnlySet = item.ModifyUnknownMapPointRoomTypes(readOnlySet);
		}
		return readOnlySet;
	}

	public static int ModifyXValue(CombatState combatState, CardModel card, int originalValue)
	{
		int num = originalValue;
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			num = item.ModifyXValue(card, num);
		}
		return num;
	}

	public static bool ShouldAddToDeck(IRunState runState, CardModel card, out AbstractModel? preventer)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (!item.ShouldAddToDeck(card))
			{
				preventer = item;
				return false;
			}
		}
		preventer = null;
		return true;
	}

	public static bool ShouldAfflict(CombatState combatState, CardModel card, AfflictionModel affliction)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldAfflict(card, affliction))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldAllowAncient(IRunState runState, Player player, AncientEventModel ancient)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (!item.ShouldAllowAncient(player, ancient))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldAllowHitting(CombatState combatState, Creature creature)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldAllowHitting(creature))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldAllowMerchantCardRemoval(IRunState runState, Player player)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (!item.ShouldAllowMerchantCardRemoval(player))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldAllowSelectingMoreCardRewards(IRunState runState, Player player, CardReward reward)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (item.ShouldAllowSelectingMoreCardRewards(player, reward))
			{
				return true;
			}
		}
		return false;
	}

	public static bool ShouldAllowTargeting(CombatState combatState, Creature target, out AbstractModel? preventer)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldAllowTargeting(target))
			{
				preventer = item;
				return false;
			}
		}
		preventer = null;
		return true;
	}

	public static bool ShouldClearBlock(CombatState combatState, Creature creature, out AbstractModel? preventer)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldClearBlock(creature))
			{
				preventer = item;
				return false;
			}
		}
		preventer = null;
		return true;
	}

	public static bool ShouldCreatureBeRemovedFromCombatAfterDeath(CombatState combatState, Creature creature)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldCreatureBeRemovedFromCombatAfterDeath(creature))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldDie(IRunState runState, CombatState? combatState, Creature creature, out AbstractModel? preventer)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(combatState))
		{
			if (!item.ShouldDie(creature))
			{
				preventer = item;
				return false;
			}
		}
		foreach (AbstractModel item2 in runState.IterateHookListeners(combatState))
		{
			if (!item2.ShouldDieLate(creature))
			{
				preventer = item2;
				return false;
			}
		}
		preventer = null;
		return true;
	}

	public static bool ShouldDisableRemainingRestSiteOptions(IRunState runState, Player player)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (!item.ShouldDisableRemainingRestSiteOptions(player))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldDraw(CombatState combatState, Player player, bool fromHandDraw, out AbstractModel? modifier)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldDraw(player, fromHandDraw))
			{
				modifier = item;
				return false;
			}
		}
		modifier = null;
		return true;
	}

	public static bool ShouldEtherealTrigger(CombatState combatState, CardModel card)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldEtherealTrigger(card))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldFlush(CombatState combatState, Player player)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldFlush(player))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldGainGold(IRunState runState, CombatState? combatState, decimal amount, Player player)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(combatState))
		{
			if (!item.ShouldGainGold(amount, player))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldGenerateTreasure(IRunState runState, Player player)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (!item.ShouldGenerateTreasure(player))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldGainStars(CombatState combatState, decimal amount, Player player)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldGainStars(amount, player))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldPayExcessEnergyCostWithStars(CombatState combatState, Player player)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (item.ShouldPayExcessEnergyCostWithStars(player))
			{
				return true;
			}
		}
		return false;
	}

	public static bool ShouldPlay(CombatState combatState, CardModel card, out AbstractModel? preventer, AutoPlayType autoPlayType)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldPlay(card, autoPlayType))
			{
				preventer = item;
				return false;
			}
		}
		preventer = null;
		return true;
	}

	public static bool ShouldPlayerResetEnergy(CombatState combatState, Player player)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (!item.ShouldPlayerResetEnergy(player))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldProceedToNextMapPoint(IRunState runState)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (!item.ShouldProceedToNextMapPoint())
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldProcurePotion(IRunState runState, CombatState? combatState, PotionModel potion, Player player)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(combatState))
		{
			if (!item.ShouldProcurePotion(potion, player))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ShouldRefillMerchantEntry(IRunState runState, MerchantEntry entry, Player player)
	{
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			if (item.ShouldRefillMerchantEntry(entry, player))
			{
				return true;
			}
		}
		return false;
	}

	public static bool ShouldStopCombatFromEnding(CombatState combatState)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (item.ShouldStopCombatFromEnding())
			{
				return true;
			}
		}
		return false;
	}

	public static bool ShouldTakeExtraTurn(CombatState combatState, Player player)
	{
		foreach (AbstractModel item in combatState.IterateHookListeners())
		{
			if (item.ShouldTakeExtraTurn(player))
			{
				return true;
			}
		}
		return false;
	}

	public static bool ShouldForcePotionReward(IRunState runState, Player player, RoomType roomType)
	{
		bool flag = false;
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			flag = flag || item.ShouldForcePotionReward(player, roomType);
		}
		return flag;
	}

	public static bool ShouldAllowFreeTravel(IRunState runState)
	{
		bool flag = false;
		foreach (AbstractModel item in runState.IterateHookListeners(null))
		{
			flag = flag || item.ShouldAllowFreeTravel();
		}
		return flag;
	}

	public static bool ShouldPowerBeRemovedOnDeath(PowerModel power)
	{
		if (power.Owner.CombatState == null)
		{
			return true;
		}
		foreach (AbstractModel item in power.CombatState.IterateHookListeners())
		{
			if (!item.ShouldPowerBeRemovedOnDeath(power))
			{
				return false;
			}
		}
		return true;
	}

	private static decimal ModifyDamageInternal(IRunState runState, CombatState? combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, ModifyDamageHookType modifyDamageHookType, out List<AbstractModel> modifiers)
	{
		decimal num = damage;
		List<AbstractModel> list = new List<AbstractModel>();
		if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Additive))
		{
			foreach (AbstractModel item in runState.IterateHookListeners(combatState))
			{
				decimal num2 = item.ModifyDamageAdditive(target, num, props, dealer, cardSource);
				num += num2;
				if (num2 != 0m)
				{
					list.Add(item);
				}
			}
		}
		if (modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative))
		{
			foreach (AbstractModel item2 in runState.IterateHookListeners(combatState))
			{
				decimal num3 = item2.ModifyDamageMultiplicative(target, num, props, dealer, cardSource);
				num *= num3;
				if (num3 != 1m)
				{
					list.Add(item2);
				}
			}
		}
		decimal num4 = decimal.MaxValue;
		foreach (AbstractModel item3 in runState.IterateHookListeners(combatState))
		{
			decimal num5 = item3.ModifyDamageCap(target, props, dealer, cardSource);
			if (num5 < num4)
			{
				num4 = num5;
				if (num > num5)
				{
					num = num5;
					list.Add(item3);
				}
			}
		}
		modifiers = list;
		return num;
	}
}
