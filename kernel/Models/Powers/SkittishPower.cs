using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class SkittishPower : PowerModel
{
	private class Data
	{
		public bool hasGainedBlockThisTurn;
	}
	
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool ShouldScaleInMultiplayer => true;


	public bool HasGainedBlockThisTurn
	{
		get
		{
			return GetInternalData<Data>().hasGainedBlockThisTurn;
		}
		private set
		{
			AssertMutable();
			GetInternalData<Data>().hasGainedBlockThisTurn = value;
		}
	}

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override void AfterAttack(AttackCommand command)
	{
		if (!HasGainedBlockThisTurn && command.DamageProps.HasFlag(ValueProp.Move) && command.ModelSource is CardModel)
		{
			DamageResult damageResult = command.Results.FirstOrDefault((DamageResult r) => r.Receiver == base.Owner);
			if (damageResult != null && damageResult.UnblockedDamage != 0)
			{
				HasGainedBlockThisTurn = true;
				CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
			}
		}
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != base.Owner.Side)
		{
			HasGainedBlockThisTurn = false;
		}
	}
}
