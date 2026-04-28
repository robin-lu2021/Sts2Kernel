using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core.Commands;

public static class OrbCmd
{
	public static void AddSlots(Player player, int amount)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		amount = Math.Min(10 - player.PlayerCombatState.OrbQueue.Capacity, amount);
		player.PlayerCombatState.OrbQueue.AddCapacity(amount);
		NCombatRoom.Instance?.GetCreatureNode(player.Creature).OrbManager?.AddSlotAnim(amount);
		return;
	}

	public static void RemoveSlots(Player player, int amount)
	{
		if (!CombatManager.Instance.IsOverOrEnding)
		{
			amount = Math.Min(player.PlayerCombatState.OrbQueue.Capacity, amount);
			player.PlayerCombatState.OrbQueue.RemoveCapacity(amount);
			NCombatRoom.Instance?.GetCreatureNode(player.Creature).OrbManager?.RemoveSlotAnim(amount);
		}
	}

	public static void Channel<T>(PlayerChoiceContext choiceContext, Player player) where T : OrbModel
	{
		Channel(choiceContext, ModelDb.Orb<T>().ToMutable(), player);
	}

	public static void Channel(PlayerChoiceContext choiceContext, OrbModel orb, Player player)
	{
		if (!CombatManager.Instance.IsOverOrEnding)
		{
			CombatState combatState = player.Creature.CombatState;
			OrbQueue orbQueue = player.PlayerCombatState.OrbQueue;
			if (player.Character.BaseOrbSlotCount == 0 && orbQueue.Capacity == 0)
			{
				AddSlots(player, 1);
			}
			orb.AssertMutable();
			orb.Owner = player;
			if (orbQueue.Orbs.Count >= orbQueue.Capacity)
			{
				EvokeNext(choiceContext, player);
			}
			if (player.PlayerCombatState.OrbQueue.TryEnqueue(orb).GetAwaiter().GetResult())
			{
				CombatManager.Instance.History.OrbChanneled(combatState, orb);
				orb.PlayChannelSfx();
				NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.AddOrbAnim();
				Hook.AfterOrbChanneled(combatState, choiceContext, player, orb);
			}
		}
	}

	public static void EvokeNext(PlayerChoiceContext choiceContext, Player player, bool dequeue = true)
	{
		OrbQueue orbQueue = player.PlayerCombatState.OrbQueue;
		if (orbQueue.Orbs.Count > 0)
		{
			OrbModel orb = orbQueue.Orbs.First();
			choiceContext.PushModel(orb);
			Evoke(choiceContext, player, orb, dequeue);
			choiceContext.PopModel(orb);
		}
	}

	public static void EvokeLast(PlayerChoiceContext choiceContext, Player player, bool dequeue = true)
	{
		OrbQueue orbQueue = player.PlayerCombatState.OrbQueue;
		if (orbQueue.Orbs.Count > 0)
		{
			OrbModel orb = orbQueue.Orbs.Last();
			choiceContext.PushModel(orb);
			Evoke(choiceContext, player, orb, dequeue);
			choiceContext.PopModel(orb);
		}
	}

	private static void Evoke(PlayerChoiceContext choiceContext, Player player, OrbModel evokedOrb, bool dequeue = true)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		OrbQueue orbQueue = player.PlayerCombatState.OrbQueue;
		if (orbQueue.Orbs.Count <= 0)
		{
			return;
		}
		bool removed = false;
		if (dequeue)
		{
			removed = orbQueue.Remove(evokedOrb);
			NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.EvokeOrbAnim(evokedOrb);
		}
		choiceContext.PushModel(evokedOrb);
		IEnumerable<Creature> targets = evokedOrb.Evoke(choiceContext).GetAwaiter().GetResult();
		choiceContext.PopModel(evokedOrb);
		if (player.Creature.CombatState != null)
		{
			Hook.AfterOrbEvoked(choiceContext, player.Creature.CombatState, evokedOrb, targets);
			if (removed)
			{
				evokedOrb.RemoveInternal();
			}
		}
	}

	public static void Passive(PlayerChoiceContext choiceContext, OrbModel orb, Creature? target)
	{
		if (!CombatManager.Instance.IsOverOrEnding)
		{
			choiceContext.PushModel(orb);
			orb.Passive(choiceContext, target);
			choiceContext.PopModel(orb);
		}
	}

	public static void Replace(OrbModel oldOrb, OrbModel newOrb, Player player)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		OrbQueue orbQueue = player.PlayerCombatState.OrbQueue;
		int idx = orbQueue.Orbs.IndexOf(oldOrb);
		newOrb.AssertMutable();
		newOrb.Owner = player;
		if (orbQueue.Remove(oldOrb))
		{
			oldOrb.RemoveInternal();
		}
		orbQueue.Insert(idx, newOrb);
		NCombatRoom.Instance?.GetCreatureNode(player.Creature)?.OrbManager?.ReplaceOrb(oldOrb, newOrb);
		return;
	}

	public static void IncreaseBaseOrbCount(Player player, int amount)
	{
		player.BaseOrbSlotCount += amount;
	}
}
