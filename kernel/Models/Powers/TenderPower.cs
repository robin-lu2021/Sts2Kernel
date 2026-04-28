using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class TenderPower : PowerModel
{
	private int _cardsPlayedThisTurn;

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override int DisplayAmount => CardsPlayedThisTurn;

	private int CardsPlayedThisTurn
	{
		get
		{
			return _cardsPlayedThisTurn;
		}
		set
		{
			AssertMutable();
			_cardsPlayedThisTurn = value;
			InvokeDisplayAmountChanged();
		}
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == base.Owner.Player)
		{
			CardsPlayedThisTurn++;
			 
			PowerCmd.Apply<StrengthPower>(base.Owner, -1m, base.Applier, null, silent: true);
			PowerCmd.Apply<DexterityPower>(base.Owner, -1m, base.Applier, null, silent: true);
		}
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == CombatSide.Player)
		{
			PowerCmd.Apply<StrengthPower>(base.Owner, CardsPlayedThisTurn, base.Applier, null, silent: true);
			PowerCmd.Apply<DexterityPower>(base.Owner, CardsPlayedThisTurn, base.Applier, null, silent: true);
			CardsPlayedThisTurn = 0;
		}
	}
}
