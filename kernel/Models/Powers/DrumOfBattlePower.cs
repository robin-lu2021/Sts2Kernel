using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class DrumOfBattlePower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;


	public override void BeforeHandDrawLate(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
	{
		if (player.Creature != base.Owner)
		{
			return;
		}
		CardPile drawPile = PileType.Draw.GetPile(base.Owner.Player);
		for (int i = 0; i < base.Amount; i++)
		{
			CardPileCmd.ShuffleIfNecessary(choiceContext, base.Owner.Player);
			CardModel cardModel = drawPile.Cards.FirstOrDefault();
			if (cardModel != null)
			{
				CardCmd.Exhaust(choiceContext, cardModel);
			}
		}
	}
}
