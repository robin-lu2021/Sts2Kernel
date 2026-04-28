using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Commands;

public static class CreatureCmd
{
	public static Creature Add<T>(CombatState combatState, string? slotName = null) where T : MonsterModel
	{
		throw new NotSupportedException("Headless kernel creature creation is not wired to the legacy Creature runtime yet.");
	}

	public static Creature Add(MonsterModel monster, CombatState combatState, CombatSide side = CombatSide.Enemy, string? slotName = null)
	{
		throw new NotSupportedException("Headless kernel creature creation is not wired to the legacy Creature runtime yet.");
	}

	public static void Add(Creature creature)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			throw new InvalidOperationException("Attempted to add a creature outside of combat.");
		}
		CombatState combatState = creature.CombatState;
		if (combatState == null)
		{
			throw new InvalidOperationException("Attempted to add a creature with no combat state.");
		}
		combatState.AddCreature(creature);
		CombatManager.Instance.AddCreature(creature);
		NCombatRoom.Instance?.AddCreature(creature);
		CombatManager.Instance.AfterCreatureAdded(creature);
		if (combatState.CurrentSide != CombatSide.Enemy && creature.IsMonster)
		{
			creature.PrepareForNextTurn(combatState.Players.Select((Player p) => p.Creature), rollNewMove: false);
		}
		Hook.AfterCreatureAddedToCombat(creature.CombatState, creature);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, Creature target, DamageVar damageVar, CardModel cardSource)
	{
		return Damage(choiceContext, target, damageVar.BaseValue, damageVar.Props, cardSource);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, CardModel cardSource)
	{
		return Damage(choiceContext, new List<Creature> { target }, amount, props, cardSource.Owner.Creature, cardSource);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, DamageVar damageVar, Creature dealer)
	{
		return Damage(choiceContext, targets, damageVar.BaseValue, damageVar.Props, dealer);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature dealer)
	{
		return Damage(choiceContext, targets, amount, props, dealer, null);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, Creature target, DamageVar damageVar, Creature dealer)
	{
		return Damage(choiceContext, target, damageVar.BaseValue, damageVar.Props, dealer);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature dealer)
	{
		return Damage(choiceContext, new global::_003C_003Ez__ReadOnlySingleElementList<Creature>(target), amount, props, dealer, null);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, Creature target, DamageVar damageVar, Creature? dealer, CardModel? cardSource)
	{
		return Damage(choiceContext, new global::_003C_003Ez__ReadOnlySingleElementList<Creature>(target), damageVar.BaseValue, damageVar.Props, dealer, cardSource);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return Damage(choiceContext, new global::_003C_003Ez__ReadOnlySingleElementList<Creature>(target), amount, props, dealer, cardSource);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, DamageVar damageVar, Creature? dealer, CardModel? cardSource)
	{
		return Damage(choiceContext, targets, damageVar.BaseValue, damageVar.Props, dealer, cardSource);
	}

	public static IEnumerable<DamageResult> Damage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (dealer != null && dealer.IsDead)
		{
			return targets.Select(t => new DamageResult(t, props)).ToList();
		}
		List<DamageResult> results = new List<DamageResult>();
		List<Creature> targetList = targets.ToList();
		if (targetList.Count == 0)
		{
			return results;
		}
		CombatState? combatState = targetList[0].CombatState;
		IRunState runState = combatState?.RunState
			?? dealer?.Player?.RunState
			?? dealer?.PetOwner?.RunState
			?? IRunState.GetFrom(targetList.Append(dealer).OfType<Creature>());
		foreach (Creature originalTarget in targetList)
		{
			if (originalTarget.IsDead)
			{
				continue;
			}
			decimal modifiedAmount = Hook.ModifyDamage(runState, combatState, originalTarget, dealer, amount, props, cardSource, ModifyDamageHookType.All, CardPreviewMode.None, out IEnumerable<AbstractModel> modifiers);
			Hook.AfterModifyingDamageAmount(runState, combatState, cardSource, modifiers);
			Hook.BeforeDamageReceived(choiceContext, runState, combatState, originalTarget, modifiedAmount, props, dealer, cardSource);
			Creature creature = originalTarget.PetOwner?.Creature ?? originalTarget;
			decimal blockedDamage = creature.DamageBlockInternal(modifiedAmount, props);
			decimal unblockedDamage = Hook.ModifyHpLostBeforeOsty(runState, combatState, originalTarget, Math.Max(modifiedAmount - blockedDamage, 0m), props, dealer, cardSource, out modifiers);
			Hook.AfterModifyingHpLostBeforeOsty(runState, combatState, modifiers);
			Creature unblockedDamageTarget = combatState == null ? originalTarget : Hook.ModifyUnblockedDamageTarget(combatState, originalTarget, unblockedDamage, props, dealer);
			unblockedDamage = Hook.ModifyHpLostAfterOsty(runState, combatState, unblockedDamageTarget, unblockedDamage, props, dealer, cardSource, out modifiers);
			Hook.AfterModifyingHpLostAfterOsty(runState, combatState, modifiers);
			DamageResult unblockedDamageResult = unblockedDamageTarget.LoseHpInternal(unblockedDamage, props);
			List<DamageResult> damageResults = new List<DamageResult>(1) { unblockedDamageResult };
			bool wasBlockBroken = originalTarget.Block <= 0 && blockedDamage > 0m;
			bool wasFullyBlocked = !props.HasFlag(ValueProp.Unblockable) && (blockedDamage > 0m || originalTarget.Block > 0) && (int)unblockedDamage == 0;
			if (originalTarget == unblockedDamageTarget)
			{
				unblockedDamageResult.BlockedDamage = (int)blockedDamage;
				unblockedDamageResult.WasBlockBroken = wasBlockBroken;
				unblockedDamageResult.WasFullyBlocked = wasFullyBlocked;
			}
			else
			{
				decimal originalTargetDamage = Hook.ModifyHpLostAfterOsty(runState, combatState, originalTarget, unblockedDamageResult.OverkillDamage, props, dealer, cardSource, out modifiers);
				Hook.AfterModifyingHpLostAfterOsty(runState, combatState, modifiers);
				DamageResult damageResult = ((!(originalTargetDamage > 0m) ? new DamageResult(originalTarget, props) : originalTarget.LoseHpInternal(originalTargetDamage, props)));
				damageResult.BlockedDamage = (int)blockedDamage;
				damageResult.WasBlockBroken = wasBlockBroken;
				damageResult.WasFullyBlocked = wasFullyBlocked;
				damageResults.Add(damageResult);
			}
			foreach (DamageResult item in damageResults)
			{
				int damage = item.UnblockedDamage + item.OverkillDamage;
				Creature receiver = item.Receiver;
				if (CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding)
				{
					CombatManager.Instance.History.DamageReceived(combatState, receiver, dealer, item, cardSource);
				}
				if (item.WasFullyBlocked)
				{
					continue;
				}
				if (damage > 0)
				{
					MapPointHistoryEntry mapPointHistoryEntry = receiver.Player?.RunState.CurrentMapPointHistoryEntry;
					if (mapPointHistoryEntry != null)
					{
						mapPointHistoryEntry.GetEntry(receiver.Player.NetId).DamageTaken += item.UnblockedDamage;
					}
				}
				if (damage <= 0)
				{
					continue;
				}
			}
			results.AddRange(damageResults);
		}
		List<Creature> killedCreatures = new List<Creature>();
		foreach (DamageResult unblockedDamageResult in results)
		{
			Creature originalTarget = unblockedDamageResult.Receiver;
			if (unblockedDamageResult.WasBlockBroken)
			{
				Hook.AfterBlockBroken(originalTarget.CombatState, originalTarget);
			}
			if (unblockedDamageResult.UnblockedDamage > 0)
			{
				Hook.AfterCurrentHpChanged(runState, combatState, originalTarget, -unblockedDamageResult.UnblockedDamage);
			}
			if (combatState != null)
			{
				Hook.AfterDamageGiven(choiceContext, combatState, dealer, unblockedDamageResult, props, originalTarget, cardSource);
			}
			if (unblockedDamageResult.WasTargetKilled && originalTarget.IsDead)
			{
				killedCreatures.Add(originalTarget);
			}
			else
			{
				Hook.AfterDamageReceived(choiceContext, runState, combatState, originalTarget, unblockedDamageResult, props, dealer, cardSource);
			}
		}
		Kill(killedCreatures);
		return results;
	}

	public static void Kill(Creature creature, bool force = false)
	{
		Kill(new global::_003C_003Ez__ReadOnlySingleElementList<Creature>(creature), force);
	}

	public static void Kill(IReadOnlyCollection<Creature> creatures, bool force = false)
	{
		if (creatures.Count == 0)
		{
			return;
		}
		IRunState runState = creatures.FirstOrDefault((Creature c) => c.IsPlayer)?.Player?.RunState;
		foreach (Creature item in creatures.ToList())
		{
			KillWithoutCheckingWinCondition(item, force);
		}
		if (runState != null && runState.Players.All((Player p) => p.Creature.IsDead))
		{
			if (CombatManager.Instance.IsInProgress)
			{
				CombatManager.Instance.LoseCombat();
			}
			if (TestMode.IsOff)
			{
				NRun.Instance.RunMusicController.StopMusic();
				SerializableRun serializableRun = RunManager.Instance.OnEnded(isVictory: false);
				NRun.Instance.ShowGameOverScreen(serializableRun);
			}
		}
		else
		{
			if (!CombatManager.Instance.IsInProgress)
			{
				return;
			}
			foreach (Creature item2 in creatures.ToList())
			{
				if (item2 != null)
				{
					CombatState combatState = item2.CombatState;
					if (combatState != null && combatState.CurrentSide == CombatSide.Player && item2.IsDead && item2.IsPlayer)
					{
						PlayerCmd.EndTurn(item2.Player, canBackOut: false);
					}
				}
			}
		}
	}

	private static void KillWithoutCheckingWinCondition(Creature creature, bool force, int recursion = 0)
	{
		if (creature.CombatState == null && !creature.IsPlayer)
		{
			return;
		}
		CombatState combatState = creature.CombatState;
		IRunState runState = IRunState.GetFrom(new global::_003C_003Ez__ReadOnlySingleElementList<Creature>(creature));
		int currentHp = creature.CurrentHp;
		if (currentHp > 0)
		{
			creature.LoseHpInternal(currentHp, ValueProp.Unblockable | ValueProp.Unpowered);
			Hook.AfterCurrentHpChanged(runState, creature.CombatState, creature, -currentHp);
		}
		Hook.BeforeDeath(runState, combatState, creature);
		AbstractModel preventer = null;
		if (force || creature.MaxHp <= 0 || Hook.ShouldDie(runState, combatState, creature, out preventer))
		{
			creature.InvokeDiedEvent();
			bool shouldRemoveFromCombat = combatState != null && Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(combatState, creature);
			float deathAnimLength = 0f;
			Hook.AfterDeath(runState, combatState, creature, wasRemovalPrevented: false, deathAnimLength);
			List<Creature> teammates = (from t in combatState?.GetTeammatesOf(creature)
				where t.IsAlive
				select t).ToList() ?? new List<Creature>();
			if (shouldRemoveFromCombat && creature.Side == CombatSide.Enemy && (combatState?.Enemies.Contains(creature) ?? false))
			{
				CombatManager.Instance.RemoveCreature(creature);
				MonsterModel monster = creature.Monster;
				if (monster != null && !monster.IsPerformingMove)
				{
					combatState.RemoveCreature(creature);
				}
			}
			bool isPrimaryEnemy = creature.IsPrimaryEnemy;
			IEnumerable<PowerModel> enumerable = creature.RemoveAllPowersAfterDeath();
			foreach (PowerModel item in enumerable)
			{
				item.AfterRemoved(creature);
			}
			if (creature.Side == CombatSide.Enemy)
			{
				if (isPrimaryEnemy && teammates.Count != 0 && teammates.All((Creature t) => t.IsSecondaryEnemy))
				{
					Kill(teammates);
				}
			}
			else if (creature.IsPlayer)
			{
				Player player = creature.Player;
				player.PlayerCombatState?.OrbQueue.Clear();
				if (player.IsOstyAlive)
				{
					Kill(player.Osty, force);
				}
				player.DeactivateHooks();
				if (combatState != null && !combatState.Players.All((Player p) => p.Creature.IsDead))
				{
					CombatManager.Instance.HandlePlayerDeath(player);
				}
			}
		}
		else
		{
			if (recursion >= 10)
			{
				throw new InvalidOperationException("Combat is ending, but something is continually preventing the last creature from being killed!");
			}
			Hook.AfterDeath(runState, combatState, creature, wasRemovalPrevented: true, 0f);
			Hook.AfterPreventingDeath(runState, combatState, preventer, creature);
			if (creature.IsDead)
			{
				KillWithoutCheckingWinCondition(creature, force, recursion + 1);
			}
		}
	}

	public static void Escape(Creature creature, bool removeCreatureNode = true)
	{
		if (creature.IsDead)
		{
			return;
		}
		creature.RemoveAllPowersInternalExcept();
		CombatManager.Instance.RemoveCreature(creature);
		creature.CombatState?.CreatureEscaped(creature);
		return;
	}

	public static decimal GainBlock(Creature creature, BlockVar blockVar, CardPlay? cardPlay, bool fast = false)
	{
		return GainBlock(creature, blockVar.BaseValue, blockVar.Props, cardPlay, fast);
	}

	public static decimal GainBlock(Creature creature, decimal amount, ValueProp props, CardPlay? cardPlay, bool fast = false)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return default(decimal);
		}
		CombatState combatState = creature.CombatState;
		Hook.BeforeBlockGained(combatState, creature, amount, props, cardPlay?.Card);
		decimal modifiedAmount = amount;
		modifiedAmount = Hook.ModifyBlock(combatState, creature, modifiedAmount, props, cardPlay?.Card, cardPlay, out IEnumerable<AbstractModel> modifiers);
		modifiedAmount = Math.Max(modifiedAmount, 0m);
		Hook.AfterModifyingBlockAmount(combatState, modifiedAmount, cardPlay?.Card, cardPlay, modifiers);
		if (modifiedAmount > 0m)
		{
			creature.GainBlockInternal(modifiedAmount);
			CombatManager.Instance.History.BlockGained(combatState, creature, (int)modifiedAmount, props, cardPlay);
		}
		Hook.AfterBlockGained(combatState, creature, modifiedAmount, props, cardPlay?.Card);
		return modifiedAmount;
	}

	public static void LoseBlock(Creature creature, decimal amount)
	{
		if (!CombatManager.Instance.IsOverOrEnding && !creature.IsDead && !(amount <= 0m))
		{
			int block = creature.Block;
			creature.LoseBlockInternal(amount);
			if (block > 0 && creature.Block <= 0)
			{
				Hook.AfterBlockBroken(creature.CombatState, creature);
			}
		}
	}

	public static void Heal(Creature creature, decimal amount, bool playAnim = true)
	{
		if (CombatManager.Instance.IsEnding && !creature.IsPlayer)
		{
			return;
		}
		bool isDead = creature.IsDead;
		decimal num = Math.Min(amount, creature.MaxHp - creature.CurrentHp);
		creature.HealInternal(amount);
		MapPointHistoryEntry mapPointHistoryEntry = creature.Player?.RunState.CurrentMapPointHistoryEntry;
		if (mapPointHistoryEntry != null && num > 0m)
		{
			mapPointHistoryEntry.GetEntry(creature.Player.NetId).HpHealed += (int)num;
		}
		if (amount > 0m && creature.CombatState != null)
		{
			Hook.AfterCurrentHpChanged(creature.Player?.RunState ?? creature.CombatState.RunState, creature.CombatState, creature, amount);
		}
	}

	public static void SetCurrentHp(Creature creature, decimal amount)
	{
		bool flag = creature.IsDead && amount > 0m;
		decimal num = creature.CurrentHp;
		creature.SetCurrentHpInternal(amount);
		if (amount != num)
		{
			Hook.AfterCurrentHpChanged(creature.Player?.RunState ?? creature.CombatState.RunState, creature.CombatState, creature, amount - num);
		}
		if (creature.IsDead)
		{
			Kill(creature);
		}
	}

	public static void GainMaxHp(Creature creature, decimal amount)
	{
		if (amount < 0m)
		{
			throw new ArgumentException("amount must be non-negative. Use LoseMaxHp for max HP loss.");
		}
		decimal num = SetMaxHp(creature, (decimal)creature.MaxHp + amount);
		MapPointHistoryEntry mapPointHistoryEntry = creature.Player?.RunState.CurrentMapPointHistoryEntry;
		if (mapPointHistoryEntry != null)
		{
			mapPointHistoryEntry.GetEntry(creature.Player.NetId).MaxHpGained += (int)num;
		}
		Heal(creature, num);
	}

	public static void LoseMaxHp(PlayerChoiceContext choiceContext, Creature creature, decimal amount, bool isFromCard)
	{
		if (amount < 0m)
		{
			throw new ArgumentException("amount must be non-negative. Use GainMaxHp for max HP gain.");
		}
		decimal newMaxHp = (decimal)creature.MaxHp - amount;
		MapPointHistoryEntry mapPointHistoryEntry = creature.Player?.RunState.CurrentMapPointHistoryEntry;
		if (mapPointHistoryEntry != null)
		{
			mapPointHistoryEntry.GetEntry(creature.Player.NetId).MaxHpLost += (int)amount;
		}
		if (newMaxHp < (decimal)creature.CurrentHp)
		{
			Damage(choiceContext, creature, (decimal)creature.CurrentHp - newMaxHp, isFromCard ? (ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move) : (ValueProp.Unblockable | ValueProp.Unpowered), null, null);
		}
		SetMaxHp(creature, Math.Max(1.0m, newMaxHp));
	}

	public static decimal SetMaxHp(Creature creature, decimal amount)
	{
		int oldMaxHp = creature.MaxHp;
		creature.SetMaxHpInternal(Math.Max(0m, amount));
		int newMaxHp = creature.MaxHp;
		if (creature.MaxHp <= 0)
		{
			Kill(creature);
		}
		return newMaxHp - oldMaxHp;
	}

	public static void SetMaxAndCurrentHp(Creature creature, decimal amount)
	{
		SetMaxHp(creature, amount);
		SetCurrentHp(creature, amount);
	}

	public static void Stun(Creature creature, string? nextMoveId = null)
	{
		Stun(creature, (IReadOnlyList<Creature> _) => { }, nextMoveId);
	}

	public static void Stun(Creature creature, Action<IReadOnlyList<Creature>> stunMove, string? nextMoveId = null)
	{
		creature.StunInternal(Wrapper, nextMoveId);
		return;
		Task Wrapper(IReadOnlyList<Creature> c)
		{
			stunMove(c);
			return Task.CompletedTask;
		}
	}
}
