using MegaCrit.Sts2.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class RazorTooth : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner)
		{
			return;
		}
		CardType type = cardPlay.Card.Type;
		if ((uint)(type - 1) > 1u)
		{
			return;
		}
		if (!cardPlay.Card.IsUpgradable)
		{
			return;
		}
		CardCmd.Upgrade(cardPlay.Card);
		return;
	}
}