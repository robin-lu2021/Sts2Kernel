using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core.Commands;

public static class PowerCmd
{
	public static IReadOnlyList<T> Apply<T>(IEnumerable<Creature> targets, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false) where T : PowerModel
	{
		List<T> powers = new List<T>();
		foreach (Creature target in targets)
		{
			T val = Apply<T>(target, amount, applier, cardSource, silent);
			if (val != null)
			{
				powers.Add(val);
			}
		}
		return powers;
	}

	public static T? Apply<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false) where T : PowerModel
	{
		if (CombatManager.Instance.IsEnding)
		{
			return null;
		}
		if (!target.CanReceivePowers)
		{
			return null;
		}
		PowerModel powerModel = ModelDb.Power<T>();
		PowerModel power;
		if (powerModel.IsInstanced || !target.HasPower<T>())
		{
			power = powerModel.ToMutable();
			Apply(power, target, amount, applier, cardSource, silent);
		}
		else
		{
			power = target.GetPower<T>();
			if (power == null)
			{
				throw new InvalidOperationException("Creature missing expected power.");
			}
			if (ModifyAmount(power, amount, applier, cardSource, silent) == 0)
			{
				power = null;
			}
		}
		return power as T;
	}

	public static void Apply(PowerModel power, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)
	{
		if (CombatManager.Instance.IsEnding || amount == 0m || !target.CanReceivePowers)
		{
			return;
		}
		CombatState combatState = target.CombatState;
		if (combatState == null)
		{
			return;
		}
		if (!power.IsInstanced && target.GetPowerById(power.Id) != null)
		{
			PowerModel power2 = target.GetPowerById(power.Id);
			if (power2 == null)
			{
				throw new InvalidOperationException("Creature missing expected power.");
			}
			ModifyAmount(power2, amount, applier, cardSource);
			return;
		}
		power.AssertMutable();
		power.Applier = applier;
		Hook.BeforePowerAmountChanged(combatState, power, amount, target, applier, cardSource);
		decimal modifiedAmount = amount;
		IEnumerable<AbstractModel>? givenModifiers = null;
		if (applier != null && combatState.ContainsCreature(applier))
		{
			modifiedAmount = Hook.ModifyPowerAmountGiven(combatState, power, applier, modifiedAmount, target, cardSource, out IEnumerable<AbstractModel> activeGivenModifiers);
			givenModifiers = activeGivenModifiers;
		}
		modifiedAmount = Hook.ModifyPowerAmountReceived(combatState, power, target, modifiedAmount, applier, out IEnumerable<AbstractModel> receivedModifiers);
		power.BeforeApplied(target, modifiedAmount, applier, cardSource);
		power.ApplyInternal(target, modifiedAmount, silent);
		if (modifiedAmount != 0m)
		{
			CombatManager.Instance.History.PowerReceived(combatState, target, power, modifiedAmount, applier);
		}
		if (target.Side == CombatSide.Player && power.Type == PowerType.Debuff)
		{
			power.SkipNextDurationTick = true;
		}
		if (givenModifiers != null)
		{
			Hook.AfterModifyingPowerAmountGiven(combatState, givenModifiers, power);
		}
		Hook.AfterModifyingPowerAmountReceived(combatState, receivedModifiers, power);
		if (modifiedAmount != 0m)
		{
			power.AfterApplied(applier, cardSource);
			Hook.AfterPowerAmountChanged(combatState, power, modifiedAmount, applier, cardSource);
		}
	}

	public static void Decrement(PowerModel power)
	{
		ModifyAmount(power, -1m, null, null);
	}

	public static void TickDownDuration(PowerModel power)
	{
		if (power.SkipNextDurationTick)
		{
			power.SkipNextDurationTick = false;
		}
		else
		{
			Decrement(power);
		}
	}

	public static int ModifyAmount(PowerModel power, decimal offset, Creature? applier, CardModel? cardSource, bool silent = false)
	{
		if (CombatManager.Instance.IsEnding)
		{
			return 0;
		}
		Creature owner = power.Owner;
		CombatState combatState = owner.CombatState;
		if (combatState == null)
		{
			return 0;
		}
		Hook.BeforePowerAmountChanged(combatState, power, offset, owner, applier, cardSource);
		decimal modifiedOffset = offset;
		IEnumerable<AbstractModel>? givenModifiers = null;
		if (applier != null && combatState.ContainsCreature(applier))
		{
			modifiedOffset = Hook.ModifyPowerAmountGiven(combatState, power, applier, modifiedOffset, owner, cardSource, out IEnumerable<AbstractModel> activeGivenModifiers);
			givenModifiers = activeGivenModifiers;
		}
		modifiedOffset = Hook.ModifyPowerAmountReceived(combatState, power, owner, modifiedOffset, applier, out IEnumerable<AbstractModel> receivedModifiers);
		CombatManager.Instance.History.PowerReceived(combatState, owner, power, modifiedOffset, applier);
		int newAmount = power.Amount + (int)modifiedOffset;
		power.SetAmount(newAmount, silent);
		if (givenModifiers != null)
		{
			Hook.AfterModifyingPowerAmountGiven(combatState, givenModifiers, power);
		}
		Hook.AfterModifyingPowerAmountReceived(combatState, receivedModifiers, power);
		if ((int)modifiedOffset != 0)
		{
			Hook.AfterPowerAmountChanged(combatState, power, modifiedOffset, applier, cardSource);
		}
		if (power.ShouldRemoveDueToAmount())
		{
			Remove(power);
		}
		return newAmount;
	}

	public static T? SetAmount<T>(Creature target, decimal amount, Creature? applier, CardModel? cardSource) where T : PowerModel
	{
		if (CombatManager.Instance.IsEnding)
		{
			return null;
		}
		T existingPower = target.GetPower<T>();
		if (existingPower == null)
		{
			return Apply<T>(target, amount, applier, cardSource);
		}
		ModifyAmount(existingPower, amount - (decimal)existingPower.Amount, applier, cardSource);
		return existingPower;
	}

	public static void Remove<T>(Creature creature) where T : PowerModel
	{
		Remove(creature.GetPower<T>());
	}

	public static void Remove(PowerModel? power)
	{
		if (power != null)
		{
			power.RemoveInternal();
			power.AfterRemoved(power.Owner);
		}
	}
}
