using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class UnsettlingLamp : RelicModel
{
	private CardModel? _triggeringCard;

	private List<PowerModel>? _doubledPowers;

	private bool _isFinishedTriggering;

	public override RelicRarity Rarity => RelicRarity.Rare;

	private CardModel? TriggeringCard
	{
		get
		{
			return _triggeringCard;
		}
		set
		{
			AssertMutable();
			_triggeringCard = value;
		}
	}

	private List<PowerModel> DoubledPowers
	{
		get
		{
			AssertMutable();
			if (_doubledPowers == null)
			{
				_doubledPowers = new List<PowerModel>();
			}
			return _doubledPowers;
		}
	}

	private bool IsFinishedTriggering
	{
		get
		{
			return _isFinishedTriggering;
		}
		set
		{
			AssertMutable();
			_isFinishedTriggering = value;
		}
	}

	public override void BeforeCombatStart()
	{
		TriggeringCard = null;
		DoubledPowers.Clear();
		IsFinishedTriggering = false;
		base.Status = RelicStatus.Active;
		return;
	}

	public override void BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
	{
		if (TriggeringCard != null)
		{
			return;
		}
		if (IsFinishedTriggering)
		{
			return;
		}
		if (cardSource == null)
		{
			return;
		}
		if (applier != base.Owner.Creature)
		{
			return;
		}
		if (target.Side == base.Owner.Creature.Side)
		{
			return;
		}
		if (!power.IsVisible)
		{
			return;
		}
		if (power.GetTypeForAmount(amount) != PowerType.Debuff)
		{
			return;
		}
		TriggeringCard = cardSource;
		DoubledPowers.Add(power);
		return;
	}

	public override decimal ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		if (TriggeringCard == null)
		{
			return amount;
		}
		if (cardSource != TriggeringCard)
		{
			return amount;
		}
		if (IsFinishedTriggering)
		{
			return amount;
		}
		if (HasDoubledTemporaryPowerSource(power))
		{
			return amount;
		}
		if (power.GetTypeForAmount(amount) != PowerType.Debuff)
		{
			return amount;
		}
		return amount * 2m;
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card != TriggeringCard)
		{
			return;
		}
		if (IsFinishedTriggering)
		{
			return;
		}
		 
		IsFinishedTriggering = true;
		base.Status = RelicStatus.Normal;
		return;
	}

	public override void AfterCombatEnd(CombatRoom room)
	{
		TriggeringCard = null;
		DoubledPowers.Clear();
		IsFinishedTriggering = false;
		base.Status = RelicStatus.Normal;
		return;
	}

	private bool HasDoubledTemporaryPowerSource(PowerModel power)
	{
		return DoubledPowers.Any((PowerModel p) => p.IsTemporaryPower && p.InternallyAppliedPowerModel?.GetType() == power.GetType());
	}
}
