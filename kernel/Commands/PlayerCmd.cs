using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Commands;

public static class PlayerCmd
{
	public static void GainEnergy(decimal amount, Player player)
	{
		if (!(amount <= 0m) && !CombatManager.Instance.IsEnding)
		{
			CombatState combatState = player.Creature.CombatState;
			IEnumerable<AbstractModel> modifiers;
			decimal finalAmount = Hook.ModifyEnergyGain(combatState, player, amount, out modifiers);
			Hook.AfterModifyingEnergyGain(combatState, modifiers);
			if (finalAmount > 0m)
			{
				player.PlayerCombatState.GainEnergy(finalAmount);
			}
		}
	}

	public static void LoseEnergy(decimal amount, Player player)
	{
		if (amount <= 0m)
		{
			return;
		}
		if (CombatManager.Instance.IsEnding)
		{
			return;
		}
		player.PlayerCombatState.LoseEnergy(amount);
		return;
	}

	public static void SetEnergy(decimal amount, Player player)
	{
		if (!CombatManager.Instance.IsEnding)
		{
			int energy = player.PlayerCombatState.Energy;
			if ((decimal)energy < amount)
			{
				GainEnergy(amount - (decimal)energy, player);
			}
			else if ((decimal)energy > amount)
			{
				LoseEnergy((decimal)energy - amount, player);
			}
		}
	}

	public static void GainStars(decimal amount, Player player)
	{
		if (!CombatManager.Instance.IsEnding && Hook.ShouldGainStars(player.Creature.CombatState, amount, player))
		{
			player.PlayerCombatState.GainStars(amount);
			Hook.AfterStarsGained(player.Creature.CombatState, (int)amount, player);
		}
	}

	public static void LoseStars(decimal amount, Player player)
	{
		if (CombatManager.Instance.IsEnding)
		{
			return;
		}
		player.PlayerCombatState.LoseStars(amount);
		return;
	}

	public static void SetStars(decimal amount, Player player)
	{
		if (!CombatManager.Instance.IsEnding)
		{
			int stars = player.PlayerCombatState.Stars;
			if ((decimal)stars < amount)
			{
				GainStars(amount - (decimal)stars, player);
			}
			else if ((decimal)stars > amount)
			{
				LoseStars((decimal)stars - amount, player);
			}
		}
	}

	public static Task GainGold(decimal amount, Player player, bool wasStolenBack = false)
	{
		if (!Hook.ShouldGainGold(player.RunState, player.Creature.CombatState, amount, player))
		{
			return Task.CompletedTask;
		}
		IRunState runState = player.RunState;
		PlayerMapPointHistoryEntry playerMapPointHistoryEntry = runState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId);
		if (playerMapPointHistoryEntry != null)
		{
			if (wasStolenBack)
			{
				playerMapPointHistoryEntry.GoldStolen -= (int)amount;
			}
			else
			{
				playerMapPointHistoryEntry.GoldGained += (int)amount;
			}
		}
		player.Gold += (int)amount;
		Hook.AfterGoldGained(runState, player);
		return Task.CompletedTask;
	}

	public static Task LoseGold(decimal amount, Player player, GoldLossType goldLossType = GoldLossType.Lost)
	{
		PlayerMapPointHistoryEntry playerMapPointHistoryEntry = player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId);
		if (playerMapPointHistoryEntry != null)
		{
			switch (goldLossType)
			{
			case GoldLossType.Spent:
				playerMapPointHistoryEntry.GoldSpent += (int)amount;
				break;
			case GoldLossType.Lost:
				playerMapPointHistoryEntry.GoldLost += (int)amount;
				break;
			case GoldLossType.Stolen:
				playerMapPointHistoryEntry.GoldStolen += (int)amount;
				break;
			}
		}
		player.Gold = int.Max(0, player.Gold - (int)amount);
		return Task.CompletedTask;
	}

	public static void SetGold(decimal amount, Player player)
	{
		int gold = player.Gold;
		if ((decimal)gold < amount)
		{
			GainGold(amount - (decimal)gold, player);
		}
		else if ((decimal)gold > amount)
		{
			LoseGold((decimal)gold - amount, player);
		}
	}

	public static void GainMaxPotionCount(int amount, Player player)
	{
		player.AddToMaxPotionCount(amount);
		return;
	}

	public static void LoseMaxPotionCount(int amount, Player player)
	{
		player.SubtractFromMaxPotionCount(amount);
		return;
	}

	public static Creature AddPet<T>(Player player) where T : MonsterModel
	{
		throw new NotSupportedException("Headless kernel pet creation is not wired to the legacy Creature runtime yet.");
	}

	public static void AddPet(Creature pet, Player player)
	{
		if (pet.CombatState == null)
		{
			throw new InvalidOperationException("Pet must already be added to a combat state.");
		}
		player.PlayerCombatState.AddPetInternal(pet);
		CreatureCmd.Add(pet);
	}

	public static void MimicRestSiteHeal(Player player, bool playSfx = true)
	{
		HealRestSiteOption.ExecuteRestSiteHeal(player, isMimicked: true);
	}

	public static void EndTurn(Player player, bool canBackOut, Action? actionDuringEnemyTurn = null)
	{
		if (!CombatManager.Instance.IsPlayerReadyToEndTurn(player))
		{
			if (LocalContext.IsMe(player))
			{
				CombatManager.Instance.OnEndedTurnLocally();
			}
			Action? wrappedAction = null;
			if (actionDuringEnemyTurn != null)
			{
				wrappedAction = actionDuringEnemyTurn;
			}
			CombatManager.Instance.SetReadyToEndTurn(player, canBackOut, wrappedAction);
		}
	}

	public static void CompleteQuest(CardModel questCard)
	{
		questCard.Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(questCard.Owner.NetId).CompletedQuests.Add(questCard.Id);
	}
}
