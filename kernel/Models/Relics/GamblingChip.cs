using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class GamblingChip : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	public override void AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == base.Owner && base.Owner.Creature.CombatState.RoundNumber <= 1)
		{
			List<CardModel> list = (CardSelectCmd.FromHandForDiscard(choiceContext, base.Owner, new CardSelectorPrefs(base.SelectionScreenPrompt, 0, 999999999), null, this).ToList());
			if (list.Count != 0)
			{
				CardCmd.DiscardAndDraw(choiceContext, list, list.Count);
			}
		}
	}
}