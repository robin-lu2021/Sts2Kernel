using System;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class AsleepPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == base.Owner && result.UnblockedDamage != 0)
		{
			if (base.Owner.HasPower<PlatingPower>())
			{
				PowerCmd.Remove(base.Owner.GetPower<PlatingPower>());
			}
			LagavulinMatriarch monster = (LagavulinMatriarch)base.Owner.Monster;
			monster.IsAwake = true;
			CreatureCmd.Stun(base.Owner, monster.WakeUpMove, "SLASH_MOVE");
			PowerCmd.Remove(this);
		}
	}

	public override void BeforeTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Side && base.Amount <= 1 && base.Owner.HasPower<PlatingPower>())
		{
			PowerCmd.Remove(base.Owner.GetPower<PlatingPower>());
		}
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Side)
		{
			PowerCmd.Decrement(this);
			if (base.Amount <= 0)
			{
				LagavulinMatriarch lagavulinMatriarch = (LagavulinMatriarch)base.Owner.Monster;
				lagavulinMatriarch.WakeUpMove(Array.Empty<Creature>());
			}
		}
	}
}
