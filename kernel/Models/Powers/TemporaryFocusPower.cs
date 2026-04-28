using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace MegaCrit.Sts2.Core.Models.Powers;

public abstract class TemporaryFocusPower : PowerModel
{
	private bool _shouldIgnoreNextInstance;

	public override PowerType Type
	{
		get
		{
			if (!IsPositive)
			{
				return PowerType.Debuff;
			}
			return PowerType.Buff;
		}
	}

	public override PowerStackType StackType => PowerStackType.Counter;

	public abstract AbstractModel OriginModel { get; }

	public override bool IsTemporaryPower => true;

	public override PowerModel InternallyAppliedPowerModel => KernelModelDb.Power<FocusPower>();

	protected virtual bool IsPositive => true;

	private int Sign
	{
		get
		{
			if (!IsPositive)
			{
				return -1;
			}
			return 1;
		}
	}

	public override LocString Title
	{
		get
		{
			AbstractModel originModel = OriginModel;
			if (!(originModel is CardModel cardModel))
			{
				if (!(originModel is PotionModel potionModel))
				{
					if (originModel is RelicModel relicModel)
					{
						return relicModel.Title;
					}
					throw new InvalidOperationException();
				}
				return potionModel.Title;
			}
			return cardModel.TitleLocString;
		}
	}

	public override LocString Description => new LocString("powers", IsPositive ? "TEMPORARY_FOCUS_POWER.description" : "TEMPORARY_FOCUS_DOWN.description");

	protected override string SmartDescriptionLocKey
	{
		get
		{
			if (!IsPositive)
			{
				return "TEMPORARY_FOCUS_DOWN.smartDescription";
			}
			return "TEMPORARY_FOCUS_POWER.smartDescription";
		}
	}

	public void IgnoreNextInstance()
	{
		_shouldIgnoreNextInstance = true;
	}

	public override void IgnoreNextTemporaryPowerInstance()
	{
		IgnoreNextInstance();
	}

	public override void BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (_shouldIgnoreNextInstance)
		{
			_shouldIgnoreNextInstance = false;
		}
		else
		{
			PowerCmd.Apply<FocusPower>(target, (decimal)Sign * amount, applier, cardSource, silent: true);
		}
	}

	public override void AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (!(amount == (decimal)base.Amount) && power == this)
		{
			if (_shouldIgnoreNextInstance)
			{
				_shouldIgnoreNextInstance = false;
			}
			else
			{
				PowerCmd.Apply<FocusPower>(base.Owner, (decimal)Sign * amount, applier, cardSource, silent: true);
			}
		}
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Side)
		{
			 
			PowerCmd.Remove(this);
			PowerCmd.Apply<FocusPower>(base.Owner, -Sign * base.Amount, base.Owner, null);
		}
	}
}
