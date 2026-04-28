using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class GhostSeed : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Shop;


	public override void AfterCardEnteredCombat(CardModel card)
	{
		if (!CanAffect(card))
		{
			return;
		}
		CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
		return;
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (!(room is CombatRoom))
		{
			return;
		}
		IEnumerable<CardModel> allCards = base.Owner.PlayerCombatState.AllCards;
		foreach (CardModel item in allCards)
		{
			if (CanAffect(item))
			{
				CardCmd.ApplyKeyword(item, CardKeyword.Ethereal);
			}
		}
		return;
	}

	public bool CanAffect(CardModel card)
	{
		if (card.Rarity == CardRarity.Basic && (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend)))
		{
			return !card.Keywords.Contains(CardKeyword.Ethereal);
		}
		return false;
	}
}