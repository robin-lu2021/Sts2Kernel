using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Begone : CardModel
{
	public Begone()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CardModel cardModel = (CardSelectCmd.FromHand(prefs: new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1), context: choiceContext, player: base.Owner, filter: null, source: this).FirstOrDefault());
		if (cardModel != null)
		{
			CardModel cardModel2 = base.CombatState.CreateCard<MinionStrike>(base.Owner);
			if (base.IsUpgraded)
			{
				CardCmd.Upgrade(cardModel2);
			}
			CardCmd.Transform(cardModel, cardModel2);
		}
	}
}
