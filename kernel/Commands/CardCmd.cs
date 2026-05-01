using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Commands;

public static class CardCmd
{
	public static void AutoPlay(PlayerChoiceContext choiceContext, CardModel card, Creature? target, AutoPlayType type = AutoPlayType.Default, bool skipXCapture = false, bool skipCardPileVisuals = false)
	{
		if (CombatManager.Instance.IsOverOrEnding || card.Owner.Creature.IsDead)
		{
			return;
		}
		CombatState combatState = card.CombatState ?? card.Owner.Creature.CombatState;
		target = NormalizeCardTarget(card, target);
		if (card.Keywords.Contains(CardKeyword.Unplayable))
		{
			MoveToResultPileWithoutPlaying(choiceContext, card);
			return;
		}
		if (!card.CanPlay(target))
		{
			MoveToResultPileWithoutPlaying(choiceContext, card);
			return;
		}
		if (card.TargetType == TargetType.AnyEnemy)
		{
			if (target == null)
			{
				target = card.Owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
			}
			if (target == null)
			{
				MoveToResultPileWithoutPlaying(choiceContext, card);
				return;
			}
		}
		if (card.TargetType == TargetType.AnyAlly)
		{
			IEnumerable<Creature> items = combatState.Allies.Where((Creature c) => c != null && c.IsAlive && c.IsPlayer && c != card.Owner.Creature);
			if (target == null)
			{
				target = card.Owner.RunState.Rng.CombatTargets.NextItem(items);
			}
			if (target == null)
			{
				MoveToResultPileWithoutPlaying(choiceContext, card);
				return;
			}
		}
		PlayerCombatState playerCombatState = card.Owner.PlayerCombatState;
		if (card.EnergyCost.CostsX && !skipXCapture)
		{
			card.EnergyCost.CapturedXValue = playerCombatState.Energy;
		}
		if (card.HasStarCostX)
		{
			card.LastStarsSpent = playerCombatState.Stars;
		}
		else
		{
			card.LastStarsSpent = Math.Max(0, card.GetStarCostWithModifiers());
		}
		if (card.Pile == null)
		{
			CardPileCmd.Add(card, PileType.Play);
		}
		ResourceInfo resources = new ResourceInfo
		{
			EnergySpent = 0,
			EnergyValue = card.EnergyCost.GetAmountToSpend(),
			StarsSpent = 0,
			StarValue = Math.Max(0, card.GetStarCostWithModifiers())
		};
		card.OnPlayWrapper(choiceContext, target, isAutoPlay: true, resources, skipCardPileVisuals);
	}

	private static Creature? NormalizeCardTarget(CardModel card, Creature? target)
	{
		return card.TargetType == TargetType.AnyEnemy || card.TargetType == TargetType.AnyAlly ? target : null;
	}

	private static void MoveToResultPileWithoutPlaying(PlayerChoiceContext choiceContext, CardModel card)
	{
		CardPileCmd.Add(card, PileType.Play);
		card.MoveToResultPileWithoutPlaying(choiceContext);
	}

	public static void Discard(PlayerChoiceContext choiceContext, CardModel card)
	{
		Discard(choiceContext, new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(card));
	}

	public static void Discard(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cards)
	{
		DiscardAndDraw(choiceContext, cards, 0);
	}

	public static void DiscardAndDraw(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cardsToDiscard, int cardsToDraw)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		List<CardModel> discardCards = cardsToDiscard.ToList();
		if (discardCards.Count == 0)
		{
			return;
		}
		CombatState combatState = discardCards[0].CombatState ?? discardCards[0].Owner.Creature.CombatState;
		List<CardModel> slyCards = new List<CardModel>();
		CardPile discardPile = PileType.Discard.GetPile(discardCards[0].Owner);
		foreach (CardModel card in discardCards)
		{
			if (card.IsSlyThisTurn)
			{
				slyCards.Add(card);
			}
			CardPileCmd.Add(card, discardPile);
		}
		discardPile.InvokeContentsChanged();
		if (cardsToDraw > 0)
		{
			CardPileCmd.Draw(choiceContext, cardsToDraw, discardCards[0].Owner);
		}
		foreach (CardModel item in slyCards)
		{
			AutoPlay(choiceContext, item, null, AutoPlayType.SlyDiscard);
		}
	}

	public static void Downgrade(CardModel card)
	{
		if (!CombatManager.Instance.IsEnding)
		{
			CardPile pile = card.Pile;
			if (pile != null && pile.Type == PileType.Deck)
			{
				card.Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(card.Owner.NetId).DowngradedCards.Add(new ModelId("cards", card.ContentId));
			}
			card.DowngradeInternal();
		}
	}

	public static void Exhaust(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal = false, bool skipVisuals = false)
	{
		if (!CombatManager.Instance.IsOverOrEnding)
		{
			CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom, null, skipVisuals);
		}
	}

	public static void Upgrade(CardModel card, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
	{
		Upgrade(new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(card), style);
	}

	public static void Upgrade(IEnumerable<CardModel> cards, CardPreviewStyle style)
	{
		if (CombatManager.Instance.IsEnding)
		{
			return;
		}
		foreach (CardModel card in cards)
		{
			if (!card.IsUpgradable)
			{
				continue;
			}
			CardPile pile = card.Pile;
			if (pile != null && pile.Type == PileType.Deck)
			{
				card.Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(card.Owner.NetId).UpgradedCards.Add(new ModelId("cards", card.ContentId));
			}
			card.UpgradeInternal();
			card.FinalizeUpgradeInternal();
		}
	}

	public static CardPileAddResult TransformToRandom(CardModel original, Rng rng, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
	{
		IEnumerable<CardModel> options = original.Pool
			.GetUnlockedCards(original.Owner.UnlockState, original.Owner.RunState.CardMultiplayerConstraint)
			.Select(CardModel.FromCore)
			.Where(c => c.Id != original.Id);
		if (original.IsInCombat)
		{
			options = options.Where(c => c.CanBeGeneratedInCombat);
		}
		CardModel replacement = rng.NextItem(options);
		if (replacement == null)
		{
			throw new InvalidOperationException($"No transformation options were available for {original.Id}.");
		}
		return Transform(original, original.CardScope.CreateCard(replacement, original.Owner), style) ?? throw new InvalidOperationException($"Failed to transform {original.Id}.");
	}

	public static CardPileAddResult? TransformTo<T>(CardModel original, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout) where T : CardModel, new()
	{
		CardModel replacement = KernelCardFactoryExtensions.CreateCard<T>(original.CardScope, original.Owner);
		return Transform(original, replacement, style);
	}

	public static CardPileAddResult? Transform(CardModel original, CardModel replacement, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
	{
		return Transform(new CardTransformation(original, replacement).Yield(), null, style).FirstOrDefault();
	}

	private static int PileIndexSort((CardTransformation, CardPile, int, CardModel) value1, (CardTransformation, CardPile, int, CardModel) value2)
	{
		if (value1.Item2.Type != value2.Item2.Type)
		{
			return value1.Item2.Type.CompareTo(value2.Item2.Type);
		}
		return value1.Item3.CompareTo(value2.Item3);
	}

	public static IEnumerable<CardPileAddResult> Transform(IEnumerable<CardTransformation> transformations, Rng? rng, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
	{
		if (CombatManager.Instance.IsEnding)
		{
			return Array.Empty<CardPileAddResult>();
		}
		CardTransformation[] transformationsArr = transformations.ToArray();
		if (transformationsArr.Length == 0)
		{
			return Array.Empty<CardPileAddResult>();
		}

		CombatState? combatState = transformationsArr[0].Original.CombatState;
		List<(CardTransformation, CardPile, int, CardModel)> transformationsWithOriginalData = new List<(CardTransformation, CardPile, int, CardModel)>();
		foreach (CardTransformation transformation in transformationsArr)
		{
			CardModel original = transformation.Original;
			original.AssertMutable();
			if (!original.IsTransformable)
			{
				throw new InvalidOperationException("Can't transform " + original.Id + " because it's un-transformable.");
			}
			CardPile pile = original.Pile ?? throw new InvalidOperationException("Can't transform " + original.Id + " because it has no pile.");
			int originalIndex = pile.Cards.IndexOf(original);
			CardModel? replacement = transformation.GetReplacement(rng);
			if (replacement == null)
			{
				throw new InvalidOperationException($"Attempting to transform un-transformable card {original}!");
			}
			original.RemoveFromCurrentPile();
			transformationsWithOriginalData.Add((transformation, pile, originalIndex, replacement));
		}
		transformationsWithOriginalData.Sort(PileIndexSort);

		List<CardPileAddResult> results = new List<CardPileAddResult>();
		foreach ((CardTransformation transformation, CardPile pile, int originalIndex, CardModel replacementModel) in transformationsWithOriginalData)
		{
			CardModel original = transformation.Original;
			IRunState runState = original.Owner.RunState;
			CardModel replacement = replacementModel;
			replacement.AssertMutable();
			if (replacement.Owner != original.Owner)
			{
				throw new InvalidOperationException($"Transformation replacement for {original.Id} had different owner from original.");
			}

			CardPileAddResult result = new CardPileAddResult
			{
				success = true,
				cardAdded = replacement,
				modifyingModels = null
			};

			if (pile.Type == PileType.Deck)
			{
				replacement = Hook.ModifyCardBeingAddedToDeck(runState, replacement, out List<AbstractModel> modifyingModels);
				result.cardAdded = replacement;
				result.modifyingModels = modifyingModels;
				replacement.FloorAddedToDeck = runState.TotalFloor;
				runState.CurrentMapPointHistoryEntry?.GetEntry(original.Owner.NetId).CardsTransformed.Add(new CardTransformationHistoryEntry(original, replacement));
				pile.AddInternal(replacement);
			}
			else
			{
				pile.AddInternal(replacement, originalIndex);
				if (combatState != null)
				{
					CombatManager.Instance.History.CardGenerated(combatState, replacement, generatedByPlayer: true);
					Hook.AfterCardEnteredCombat(combatState, replacement);
				}
			}

			Hook.AfterCardChangedPiles(runState, combatState, replacement, pile.Type, null);
			pile.InvokeCardAddFinished();
			original.AfterTransformedFrom();
			replacement.AfterTransformedTo();
			results.Add(result);
		}

		for (int i = 0; i < results.Count; i++)
		{
			CardPileAddResult result = results[i];
			CombatState? resultCombatState = result.cardAdded.CombatState ?? combatState;
			if (result.success && result.cardAdded.Pile?.Type.IsCombatPile() == true && resultCombatState != null)
			{
				Hook.AfterCardGeneratedForCombat(resultCombatState, result.cardAdded, addedByPlayer: true);
			}
			transformationsWithOriginalData[i].Item1.Original.RemoveFromState();
		}
		return results;
	}

	public static T? Enchant<T>(CardModel card, decimal amount) where T : EnchantmentModel
	{
		return Enchant(ModelDb.Enchantment<T>().ToMutable(), card, amount) as T;
	}

	public static EnchantmentModel? Enchant(EnchantmentModel enchantment, CardModel card, decimal amount)
	{
		enchantment.AssertMutable();
		if (!enchantment.CanEnchant(card))
		{
			throw new InvalidOperationException($"Cannot enchant {card.Id} with {enchantment.Id}.");
		}
		if (card.Enchantment == null)
		{
			card.EnchantInternal(enchantment, amount);
			enchantment.ModifyCard();
		}
		else
		{
			if (!(card.Enchantment.GetType() == enchantment.GetType()))
			{
				throw new InvalidOperationException($"Cannot enchant {card.Id} with {enchantment.Id} because it already has enchantment {card.Enchantment.Id}.");
			}
			card.Enchantment.Amount += (int)amount;
		}
		card.FinalizeUpgradeInternal();
		CardPile pile = card.Pile;
		if (pile != null && pile.Type == PileType.Deck)
		{
			card.Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(card.Owner.NetId).CardsEnchanted.Add(new CardEnchantmentHistoryEntry(card, enchantment.Id));
		}
		return card.Enchantment;
	}

	public static void ClearEnchantment(CardModel card)
	{
		card.ClearEnchantmentInternal();
	}

	public static IEnumerable<T> AfflictAndPreview<T>(IEnumerable<CardModel> cards, decimal amount, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout) where T : AfflictionModel
	{
		List<T> afflictions = new List<T>();
		List<CardModel> cardList = new List<CardModel>();
		foreach (CardModel card in cards)
		{
			T val = Afflict<T>(card, amount);
			if (val != null)
			{
				afflictions.Add(val);
				cardList.Add(card);
			}
		}
		if (cardList.Count > 0 && style != CardPreviewStyle.None)
		{
			if (cardList.Any((CardModel c) => c.Owner != cardList[0].Owner))
			{
				throw new InvalidOperationException("All cards passed to AfflictAndPreview must have the same owner!");
			}
		}
		return afflictions;
	}

	public static T? Afflict<T>(CardModel card, decimal amount) where T : AfflictionModel
	{
		return (Afflict(ModelDb.Affliction<T>().ToMutable(), card, amount) as T);
	}

	public static AfflictionModel? Afflict(AfflictionModel affliction, CardModel card, decimal amount)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			CardPile pile = card.Pile;
			if (pile != null && pile.IsCombatPile)
			{
				return null;
			}
		}
		affliction.AssertMutable();
		CombatState combatState = card.CombatState ?? card.Owner.Creature.CombatState;
		if (combatState == null)
		{
			return null;
		}
		if (!affliction.CanAfflict(card))
		{
			return null;
		}
		if (card.Affliction == null)
		{
			card.AfflictInternal(affliction, amount);
			affliction.AfterApplied();
		}
		else
		{
			if (!(card.Affliction.GetType() == affliction.GetType()))
			{
				throw new InvalidOperationException($"Cannot afflict {card.Id} with {affliction.Id} because it already has affliction {card.Affliction.Id}.");
			}
			card.Affliction.Amount += (int)amount;
		}
		return card.Affliction;
	}

	public static void ClearAffliction(CardModel card)
	{
		card.ClearAfflictionInternal();
	}

	public static void ApplyKeyword(CardModel card, params CardKeyword[] keywords)
	{
		foreach (CardKeyword keyword in keywords)
		{
			card.AddKeyword(keyword);
		}
	}

	public static void RemoveKeyword(CardModel card, params CardKeyword[] keywords)
	{
		foreach (CardKeyword keyword in keywords)
		{
			card.RemoveKeyword(keyword);
		}
	}

	public static void ApplySingleTurnSly(CardModel card)
	{
		card.GiveSingleTurnSly();
	}

	public static void Preview(CardModel card, float time = 1.2f, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
	{
		return;
	}

	public static void Preview(IReadOnlyList<CardModel> cards, float time = 1.2f, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
	{
	}

	public static void PreviewCardPileAdd(CardPileAddResult result, float time = 1.2f, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
	{
	}

	public static void PreviewCardPileAdd(IReadOnlyList<CardPileAddResult> results, float time = 1.2f, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
	{
	}

	private static void PreviewInternal(CardModel card, bool isAddingCardsToPile, IEnumerable<RelicModel>? relicsToFlash = null, float time = 1.2f, CardPreviewStyle style = CardPreviewStyle.HorizontalLayout)
	{
		return;
	}
}
