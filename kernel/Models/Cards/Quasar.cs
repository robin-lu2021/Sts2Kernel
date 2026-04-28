using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Quasar : CardModel
{
	public override int CanonicalStarCost => 2;

	public Quasar()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> cards = KernelCardFactoryExtensions.GetDistinctForCombat(base.Owner, ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint), 3, base.Owner.RunState.Rng.CombatCardGeneration).ToList();
		if (base.IsUpgraded)
		{
			CardCmd.Upgrade(cards, CardPreviewStyle.HorizontalLayout);
		}
		CardModel cardModel = CardSelectCmd.FromChooseACardScreen(choiceContext, cards, base.Owner, canSkip: true);
		if (cardModel != null)
		{
			CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Hand, addedByPlayer: true);
		}
	}
}

