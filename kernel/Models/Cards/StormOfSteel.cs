using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class StormOfSteel : CardModel
{

	public StormOfSteel()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		IEnumerable<CardModel> enumerable = PileType.Hand.GetPile(base.Owner).Cards.ToList();
		int handSize = enumerable.Count();
		CardCmd.Discard(choiceContext, enumerable);
		IEnumerable<CardModel> enumerable2 = Shiv.CreateInHand(base.Owner, handSize, base.CombatState);
		if (!base.IsUpgraded)
		{
			return;
		}
		foreach (CardModel item in enumerable2)
		{
			CardCmd.Upgrade(item);
		}
	}
}
