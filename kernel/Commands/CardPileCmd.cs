using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Commands;

public static class CardPileCmd
{
	public static void RemoveFromDeck(CardModel card, bool showPreview = true)
	{
		RemoveFromDeck(new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(card), showPreview);
	}

	public static void RemoveFromDeck(IReadOnlyList<CardModel> cards, bool showPreview = true)
	{
		foreach (CardModel card in cards)
		{
			if (card.Pile?.Type != PileType.Deck)
			{
				throw new InvalidOperationException("You cannot remove a card that is not in the deck.");
			}
			card.RemoveFromCurrentPile();
			card.RemoveFromState();
		}
	}

	public static void RemoveFromCombat(CardModel card, bool skipVisuals = false)
	{
		RemoveFromCombat(new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(card), skipVisuals);
	}

	public static void RemoveFromCombat(IEnumerable<CardModel> cards, bool skipVisuals = false)
	{
		List<CardModel> list = cards.ToList();
		if (list.Count == 0)
		{
			return;
		}
		foreach (CardModel card in list)
		{
			CardPile? oldPile = card.Pile;
			if (oldPile == null || !oldPile.IsCombatPile)
			{
				throw new InvalidOperationException("Card must be in a combat pile for it to be removed");
			}
			card.RemoveFromCurrentPile();
			card.RemoveFromState();
		}
	}

	public static CardPileAddResult AddGeneratedCardToCombat(CardModel card, PileType newPileType, bool addedByPlayer, CardPilePosition position = CardPilePosition.Bottom)
	{
		return AddGeneratedCardsToCombat(new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(card), newPileType, addedByPlayer, position)[0];
	}

	public static IReadOnlyList<CardPileAddResult> AddGeneratedCardsToCombat(IEnumerable<CardModel> cards, PileType newPileType, bool addedByPlayer, CardPilePosition position = CardPilePosition.Bottom)
	{
		List<CardModel> list = cards.ToList();
		if (list.Count == 0 || !CombatManager.Instance.IsInProgress)
		{
			return Array.Empty<CardPileAddResult>();
		}
		if (list.Any(c => c.Pile != null))
		{
			throw new InvalidOperationException("You are not allowed to generate cards that already have a pile");
		}
		if (!newPileType.IsCombatPile())
		{
			throw new InvalidOperationException("You are not allowed to add generated cards to a non combat pile");
		}
		CombatState? combatState = list[0].Owner.Creature.CombatState;
		if (combatState == null)
		{
			return Array.Empty<CardPileAddResult>();
		}
		List<CardPileAddResult> results = new();
		foreach (CardModel card in list)
		{
			results.Add(Add(card, newPileType.GetPile(card.Owner), position));
		}
		return results;
	}

	public static CardPileAddResult Add(CardModel card, PileType newPileType, CardPilePosition position = CardPilePosition.Bottom, AbstractModel? source = null, bool skipVisuals = false)
	{
		if (card.Owner == null)
		{
			throw new InvalidOperationException($"Attempted to add card {card} to pile, but it has no owner!");
		}
		return Add(card, newPileType.GetPile(card.Owner), position, source, skipVisuals);
	}

	public static CardPileAddResult Add(CardModel card, CardPile newPile, CardPilePosition position = CardPilePosition.Bottom, AbstractModel? source = null, bool skipVisuals = false)
	{
		return Add(new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(card), newPile, position, source, skipVisuals)[0];
	}

	public static IReadOnlyList<CardPileAddResult> Add(IEnumerable<CardModel> cards, PileType newPileType, CardPilePosition position = CardPilePosition.Bottom, AbstractModel? source = null, bool skipVisuals = false)
	{
		List<CardModel> list = cards.ToList();
		if (list.Count == 0)
		{
			return Array.Empty<CardPileAddResult>();
		}
		return Add(list, newPileType.GetPile(list[0].Owner), position, source, skipVisuals);
	}

	public static IReadOnlyList<CardPileAddResult> Add(IEnumerable<CardModel> cards, CardPile newPile, CardPilePosition position = CardPilePosition.Bottom, AbstractModel? source = null, bool skipVisuals = false)
	{
		List<CardModel> list = cards.ToList();
		if (list.Count == 0)
		{
			return Array.Empty<CardPileAddResult>();
		}
		if (newPile.IsCombatPile && CombatManager.Instance.IsEnding)
		{
			return list.Select(c => new CardPileAddResult { cardAdded = c, success = false }).ToList();
		}

		List<CardPileAddResult> results = new();
		Player? owningPlayer = null;

		foreach (CardModel card in list)
		{
			if (card.Owner == null)
			{
				throw new InvalidOperationException(card.Id + " has no owner.");
			}
			Creature creature = card.Owner.Creature;
			if (card.HasBeenRemovedFromState || creature.IsDead || (card.IsInCombat && creature.CombatState == null))
			{
				CardPileAddResult item = new CardPileAddResult
				{
					success = false,
					cardAdded = card,
					oldPile = card.Pile,
					modifyingModels = null
				};
				results.Add(item);
				continue;
			}
			if (owningPlayer == null)
			{
				owningPlayer = card.Owner;
			}
			else if (owningPlayer != card.Owner)
			{
				throw new InvalidOperationException("Tried to add cards with different owners to the same pile!");
			}

			if (card.UpgradePreviewType.IsPreview())
			{
				throw new InvalidOperationException("A card preview cannot be added to a pile.");
			}
			results.Add(new CardPileAddResult
			{
				success = true,
				cardAdded = card,
				oldPile = card.Pile,
				modifyingModels = null
			});
		}

		if (owningPlayer == null)
		{
			return results;
		}

		if (newPile.Type == PileType.Deck)
		{
			for (int i = 0; i < results.Count; i++)
			{
				CardPileAddResult result = results[i];
				IRunState runState = owningPlayer.RunState;
				result.cardAdded.FloorAddedToDeck = runState.TotalFloor;
				results[i] = result;
			}
		}

		foreach (CardPileAddResult result in results.Where(r => r.success))
		{
			CardModel card = result.cardAdded;
			CardPile? oldPile = result.oldPile ?? card.Owner.Piles.FirstOrDefault((CardPile pile) => pile.Cards.Contains(card));
			CardPile targetPile = newPile;

			if (targetPile.Type == PileType.Hand && targetPile.Cards.Count >= 10)
			{
				targetPile = PileType.Discard.GetPile(card.Owner);
			}

			CardModel addedCard = card;
			if (oldPile != null)
			{
				card.RemoveFromCurrentPile(skipVisuals);
			}

			int insertIndex = position switch
			{
				CardPilePosition.Bottom => -1,
				CardPilePosition.Top => 0,
				CardPilePosition.Random => card.Owner.RunState.Rng.Shuffle.NextInt(targetPile.Cards.Count + 1),
				_ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
			};

			targetPile.AddInternal(addedCard, insertIndex, skipVisuals);

			if (targetPile.Type == PileType.Hand && targetPile != newPile)
			{
				ThinkCmd.Play(new LocString("combat_messages", "HAND_FULL"), owningPlayer.Creature, 2.0);
			}
		}

		return results;
	}

	public static void AddDuringManualCardPlay(CardModel card)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		CombatState? combatState = card.Owner.Creature.CombatState;
		if (combatState == null)
		{
			throw new InvalidOperationException(card.Id + " must be added to a CombatState before playing it.");
		}
		Add(card, PileType.Play);
	}

	public static CardModel? Draw(PlayerChoiceContext choiceContext, Player player)
	{
		return Draw(choiceContext, 1m, player).FirstOrDefault();
	}

	public static IEnumerable<CardModel> Draw(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw = false)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return Array.Empty<CardModel>();
		}

		CombatState combatState = player.Creature.CombatState;
		List<CardModel> result = new();
		CardPile hand = PileType.Hand.GetPile(player);
		CardPile drawPile = PileType.Draw.GetPile(player);
		int drawsRequested = count > 0m ? (int)Math.Ceiling(count) : 0;
		if (drawsRequested == 0)
		{
			return result;
		}

		for (int i = 0; i < drawsRequested; i++)
		{
			if (hand.Cards.Count >= 10)
			{
				break;
			}
			if (!CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot(player))
			{
				break;
			}
			ShuffleIfNecessary(choiceContext, player);
			CardModel? card = drawPile.Cards.OfType<CardModel>().FirstOrDefault();
			if (card == null)
			{
				break;
			}
			result.Add(card);
			Add(card, hand);
			card.InvokeDrawn();
		}
		return result;
	}

	public static void Shuffle(PlayerChoiceContext choiceContext, Player player)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		CardPile drawPile = PileType.Draw.GetPile(player);
		CardPile discardPile = PileType.Discard.GetPile(player);
		List<CardModel> list = discardPile.Cards.OfType<CardModel>().ToList();
		HashSet<CardModel> drawPileCards = drawPile.Cards.OfType<CardModel>().ToHashSet();
		foreach (CardModel item in drawPileCards)
		{
			drawPile.RemoveInternal(item, silent: true);
			list.Add(item);
		}
		list.StableShuffle(player.RunState.Rng.Shuffle);
		foreach (CardModel item2 in list)
		{
			if (!drawPileCards.Contains(item2))
			{
				Add(item2, drawPile, skipVisuals: true);
			}
			else
			{
				drawPile.AddInternal(item2, -1, silent: true);
			}
		}
	}

	public static void AutoPlayFromDrawPile(PlayerChoiceContext choiceContext, Player player, int count, CardPilePosition position, bool forceExhaust)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		List<CardModel> cards = new(count);
		CardPile drawPile = PileType.Draw.GetPile(player);
		for (int i = 0; i < count; i++)
		{
			ShuffleIfNecessary(choiceContext, player);
			CardModel? cardModel = position switch
			{
				CardPilePosition.Bottom => drawPile.Cards.OfType<CardModel>().LastOrDefault(),
				CardPilePosition.Top => drawPile.Cards.OfType<CardModel>().FirstOrDefault(),
				CardPilePosition.Random => player.RunState.Rng.CombatCardSelection.NextItem(drawPile.Cards.OfType<CardModel>()),
				_ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
			};
			if (cardModel == null)
			{
				break;
			}
			cards.Add(cardModel);
			Add(cardModel, PileType.Play);
		}

		foreach (CardModel item in cards)
		{
			if (!item.Owner.Creature.IsDead)
			{
				item.ExhaustOnNextPlay = forceExhaust;
				CardCmd.AutoPlay(choiceContext, item, null);
				continue;
			}
			break;
		}
	}

	public static void ShuffleIfNecessary(PlayerChoiceContext choiceContext, Player player)
	{
		CardPile drawPile = PileType.Draw.GetPile(player);
		CardPile discardPile = PileType.Discard.GetPile(player);
		if (!drawPile.Cards.Any() && discardPile.Cards.Any())
		{
			Shuffle(choiceContext, player);
		}
	}

	public static void AddToCombatAndPreview<T>(IEnumerable<Creature> targets, PileType pileType, int count, bool addedByPlayer, CardPilePosition position = CardPilePosition.Bottom) where T : CardModel, new()
	{
		foreach (Creature target in targets)
		{
			AddToCombatAndPreview<T>(target, pileType, count, addedByPlayer, position);
		}
	}

	public static void AddToCombatAndPreview<T>(Creature target, PileType pileType, int count, bool addedByPlayer, CardPilePosition position = CardPilePosition.Bottom) where T : CardModel, new()
	{
		Player player = target.Player ?? target.PetOwner!;
		if (player.Creature.IsDead)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			CardModel card = KernelCardFactoryExtensions.CreateCard<T>(target.CombatState, player);
			AddGeneratedCardToCombat(card, pileType, addedByPlayer, position);
		}
	}

	public static void AddCurseToDeck<T>(Player owner) where T : CardModel
	{
		AddCursesToDeck(new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(KernelModelDb.Card<T>()), owner);
	}

	public static void AddCursesToDeck(IEnumerable<CardModel> curses, Player owner)
	{
		foreach (CardModel curse in curses)
		{
			if (curse.Type != CardType.Curse)
			{
				throw new ArgumentException(curse.Id + " is not a curse");
			}
			CardModel card = owner.RunState.CreateCard(curse, owner);
			Add(card, PileType.Deck);
		}
	}

	private static bool CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot(Player player)
	{
		if (PileType.Draw.GetPile(player).Cards.Count + PileType.Discard.GetPile(player).Cards.Count == 0)
		{
			ThinkCmd.Play(new LocString("combat_messages", "NO_DRAW"), player.Creature, 2.0);
			return false;
		}
		if (PileType.Hand.GetPile(player).Cards.Count >= 10)
		{
			ThinkCmd.Play(new LocString("combat_messages", "HAND_FULL"), player.Creature, 2.0);
			return false;
		}
		return true;
	}
}
