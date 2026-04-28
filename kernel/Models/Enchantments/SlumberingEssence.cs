using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Enchantments;

public sealed class SlumberingEssence : EnchantmentModel
{
	public override void BeforeFlush(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Card.Owner)
		{
			return;
		}
		CardPile? pile = base.Card.Pile;
		if (pile == null || pile.Type != PileType.Hand)
		{
			return;
		}
		base.Card.EnergyCost.AddUntilPlayed(-1);
	}
}
