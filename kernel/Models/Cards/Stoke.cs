using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Stoke : CardModel
{
	public Stoke()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> list = PileType.Hand.GetPile(base.Owner).Cards.ToList();
		int exhaustCount = list.Count;
		foreach (CardModel item in list)
		{
			CardCmd.Exhaust(choiceContext, item);
		}
		List<CardModel> cards = CardFactory.GetForCombat(base.Owner, base.Owner.Character.CardPool.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint), exhaustCount, base.Owner.RunState.Rng.CombatCardGeneration).ToList();
		if (base.IsUpgraded)
		{
			CardCmd.Upgrade(cards, CardPreviewStyle.None);
		}
		CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
	}
}
