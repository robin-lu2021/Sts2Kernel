using System;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class SlumberPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == base.Owner && result.UnblockedDamage != 0)
		{
			PowerCmd.Decrement(this);
			if (base.Amount <= 0)
			{
				SlumberingBeetle slumberingBeetle = (SlumberingBeetle)base.Owner.Monster;
				CreatureCmd.Stun(base.Owner, slumberingBeetle.WakeUpMove, "ROLL_OUT_MOVE");
			}
		}
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Side)
		{
			PowerCmd.Decrement(this);
			if (base.Amount <= 0)
			{
				SlumberingBeetle slumberingBeetle = (SlumberingBeetle)base.Owner.Monster;
				slumberingBeetle.WakeUpMove(Array.Empty<Creature>());
			}
		}
	}
}
