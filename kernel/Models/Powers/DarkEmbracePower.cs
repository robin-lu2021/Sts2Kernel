using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class DarkEmbracePower : PowerModel
{
	private class Data
	{
		public int etherealCount;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;


	protected override object InitInternalData()
	{
		return new Data();
	}

	public override void AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (card.Owner.Creature == base.Owner)
		{
			if (causedByEthereal)
			{
				GetInternalData<Data>().etherealCount++;
			}
			else
			{
				CardPileCmd.Draw(choiceContext, base.Amount, base.Owner.Player);
			}
		}
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == CombatSide.Player)
		{
			Data data = GetInternalData<Data>();
			CardPileCmd.Draw(choiceContext, base.Amount * data.etherealCount, base.Owner.Player);
			data.etherealCount = 0;
		}
	}
}
