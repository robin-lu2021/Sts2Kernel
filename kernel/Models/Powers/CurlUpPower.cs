using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class CurlUpPower : PowerModel
{
	private class Data
	{
		public CardModel? playedCard;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool ShouldScaleInMultiplayer => true;


	protected override object InitInternalData()
	{
		return new Data();
	}

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult _, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != base.Owner)
		{
			return;
		}
		if (!props.IsPoweredAttack())
		{
			return;
		}
		if (cardSource == null)
		{
			return;
		}
		if (GetInternalData<Data>().playedCard != null && cardSource != GetInternalData<Data>().playedCard)
		{
			return;
		}
		GetInternalData<Data>().playedCard = cardSource;
		return;
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card == GetInternalData<Data>().playedCard)
		{
			GetInternalData<Data>().playedCard = null;
			CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
			if (base.Owner.Monster is LouseProgenitor louseProgenitor)
			{
				louseProgenitor.Curled = true;
			}
			PowerCmd.Remove(this);
		}
	}
}
