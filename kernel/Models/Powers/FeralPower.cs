using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class FeralPower : PowerModel
{
	private class Data
	{
		public int zeroCostAttacksPlayed;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override int DisplayAmount => Math.Max(0, base.Amount - GetInternalData<Data>().zeroCostAttacksPlayed);

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override void AfterApplied(Creature? applier, CardModel? cardSource)
	{
		SetZeroCostAttacksPlayed(CombatManager.Instance.History.Entries.OfType<CardPlayStartedEntry>().Count((CardPlayStartedEntry e) => e.CardPlay.Card.Type == CardType.Attack && e.CardPlay.Card.Owner.Creature == base.Owner && e.CardPlay.Resources.EnergyValue == 0 && e.HappenedThisTurn(base.CombatState)));
		return;
	}

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
	{
		if (card.Owner.Creature != base.Owner)
		{
			return (pileType, position);
		}
		if (card.Type != CardType.Attack)
		{
			return (pileType, position);
		}
		if (resources.EnergyValue > 0)
		{
			return (pileType, position);
		}
		if (GetInternalData<Data>().zeroCostAttacksPlayed >= base.Amount)
		{
			return (pileType, position);
		}
		return (PileType.Hand, CardPilePosition.Top);
	}

	public override void AfterModifyingCardPlayResultPileOrPosition(CardModel card, PileType pileType, CardPilePosition position)
	{
		SetZeroCostAttacksPlayed(GetInternalData<Data>().zeroCostAttacksPlayed + 1);
		InvokeDisplayAmountChanged();
		return;
	}

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side != base.Owner.Side)
		{
			return;
		}
		SetZeroCostAttacksPlayed(0);
		InvokeDisplayAmountChanged();
		return;
	}

	private void SetZeroCostAttacksPlayed(int value)
	{
		GetInternalData<Data>().zeroCostAttacksPlayed = value;
		InvokeDisplayAmountChanged();
	}
}
