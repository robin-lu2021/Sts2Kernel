using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Commands;

public static class ForgeCmd
{
	public static IEnumerable<SovereignBlade> Forge(decimal amount, Player player, AbstractModel? source)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return Array.Empty<SovereignBlade>();
		}
		List<SovereignBlade> blades = new List<SovereignBlade>(GetSovereignBlades(player, includeExhausted: false));
		if (blades.Count == 0)
		{
			SovereignBlade sovereignBlade = new SovereignBlade();
			sovereignBlade.Owner = player;
			sovereignBlade.CreatedThroughForge = true;
			sovereignBlade.AfterCreated();
			CardPileCmd.AddGeneratedCardToCombat(sovereignBlade, PileType.Hand, addedByPlayer: true);
			blades.Add(sovereignBlade);
		}
		IncreaseSovereignBladeDamage(amount, player);
		Hook.AfterForge(player.Creature.CombatState, amount, player, null);
		return blades;
	}

	private static void IncreaseSovereignBladeDamage(decimal amount, Player player)
	{
		foreach (SovereignBlade item in GetSovereignBlades(player, includeExhausted: true))
		{
			item.AddDamage(amount);
		}
	}

	private static IEnumerable<SovereignBlade> GetSovereignBlades(Player player, bool includeExhausted)
	{
		return player.PlayerCombatState.AllCards.Where(c =>
		{
			if (c.IsDupe)
			{
				return false;
			}
			if (!includeExhausted)
			{
				CardPile? pile = c.Pile;
				if (pile != null && pile.Type == PileType.Exhaust)
				{
					return false;
				}
			}
			return true;
		}).OfType<SovereignBlade>();
	}

	public static void PlayCombatRoomForgeVfx(Player player, CardModel card)
	{
	}
}
