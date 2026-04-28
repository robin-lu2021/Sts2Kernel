using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core;

public static class KernelCardFactoryExtensions
{
	public static IEnumerable<CardModel> FilterForCombat(IEnumerable<CardModel> cards)
	{
		return cards.Where(c => c.CanBeGeneratedInCombat && c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient && c.Rarity != CardRarity.Event)
			.GroupBy(c => c.Id.Entry, StringComparer.Ordinal)
			.Select(g => g.First());
	}

	public static IEnumerable<CardModel> GetDistinctForCombat(Player player, IEnumerable<CardModel> cards, int count, Rng rng)
	{
		if (player == null)
		{
			throw new ArgumentNullException(nameof(player));
		}
		List<CardModel> filtered = FilterForCombat(cards).ToList();
		if (player.RunState.Players.Count > 1)
		{
			filtered = filtered.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly).ToList();
		}
		else
		{
			filtered = filtered.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly).ToList();
		}
		return filtered.TakeRandom(count, rng).Select(c => player.Creature.CombatState.CreateCard(c, player));
	}

	public static IEnumerable<CardModel> GetForCombat(Player player, IEnumerable<CardModel> cards, int count, Rng rng)
	{
		if (player == null)
		{
			throw new ArgumentNullException(nameof(player));
		}
		List<CardModel> options = FilterForCombat(cards).ToList();
		if (player.RunState.Players.Count > 1)
		{
			options = options.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly).ToList();
		}
		else
		{
			options = options.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly).ToList();
		}
		List<CardModel> results = new List<CardModel>();
		for (int i = 0; i < count; i++)
		{
			CardModel canonical = rng.NextItem(options);
			if (canonical == null)
			{
				break;
			}
			results.Add(player.Creature.CombatState.CreateCard(canonical, player));
		}
		return results;
	}

	public static T CreateCard<T>(this ICardScope scope, Player owner) where T : CardModel, new()
	{
		if (scope == null)
		{
			throw new ArgumentNullException(nameof(scope));
		}
		return (T)scope.CreateCard(new T(), owner);
	}

	public static CardModel CreateCard(this ICardScope scope, CardModel canonicalCard, Player owner)
	{
		if (scope == null)
		{
			throw new ArgumentNullException(nameof(scope));
		}
		if (canonicalCard == null)
		{
			throw new ArgumentNullException(nameof(canonicalCard));
		}
		if (owner == null)
		{
			throw new ArgumentNullException(nameof(owner));
		}

		CardModel card = canonicalCard.ToMutable();
		card.Owner = owner;
		card.AfterCreated();
		return card;
	}

	public static T CreateCard<T>(this CombatState combatState, Player owner) where T : CardModel, new()
	{
		if (combatState == null)
		{
			throw new ArgumentNullException(nameof(combatState));
		}
		T card = combatState.RunState.CreateCard<T>(owner);
		card.SetCurrentPile(PileType.None);
		return card;
	}

	public static CardModel CreateCard(this CombatState combatState, CardModel canonicalCard, Player owner)
	{
		if (combatState == null)
		{
			throw new ArgumentNullException(nameof(combatState));
		}
		CardModel card = combatState.RunState.CreateCard(canonicalCard, owner);
		card.SetCurrentPile(PileType.None);
		return card;
	}

	public static T CreateCard<T>(this RunState runState, Player owner) where T : CardModel, new()
	{
		if (runState == null)
		{
			throw new ArgumentNullException(nameof(runState));
		}
		return (T)((ICardScope)runState).CreateCard(new T(), owner);
	}

	public static CardModel CreateCard(this RunState runState, CardModel canonicalCard, Player owner)
	{
		if (runState == null)
		{
			throw new ArgumentNullException(nameof(runState));
		}
		return ((ICardScope)runState).CreateCard(canonicalCard, owner);
	}
}
