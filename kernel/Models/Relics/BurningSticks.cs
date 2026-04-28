using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BurningSticks : RelicModel
{
	private bool _wasUsedThisCombat;

	public override RelicRarity Rarity => RelicRarity.Shop;

	private bool WasUsedThisCombat
	{
		get
		{
			return _wasUsedThisCombat;
		}
		set
		{
			AssertMutable();
			_wasUsedThisCombat = value;
		}
	}


	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (!(room is CombatRoom))
		{
			return;
		}
		WasUsedThisCombat = false;
		base.Status = RelicStatus.Active;
		return;
	}

	public override void AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (card.Owner == base.Owner && !WasUsedThisCombat && card.Type == CardType.Skill)
		{
			 
			CardModel card2 = card.CreateClone();
			CardPileCmd.AddGeneratedCardToCombat(card2, PileType.Hand, addedByPlayer: true);
			base.Status = RelicStatus.Normal;
			WasUsedThisCombat = true;
		}
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		WasUsedThisCombat = false;
		base.Status = RelicStatus.Normal;
		return;
	}
}