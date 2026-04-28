using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class FlutterPower : PowerModel
{
	private const string _damageDecreaseKey = "DamageDecrease";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool ShouldScaleInMultiplayer => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("DamageDecrease", 50m));

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != base.Owner)
		{
			return 1m;
		}
		if (!props.IsPoweredAttack())
		{
			return 1m;
		}
		return base.DynamicVars["DamageDecrease"].BaseValue / 100m;
	}

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == base.Owner && result.UnblockedDamage != 0 && props.IsPoweredAttack())
		{
			PowerCmd.Decrement(this);
			if (base.Amount <= 0)
			{
				string nextState = base.Owner.Monster.MoveStateMachine.StateLog.Last().GetNextState(base.Owner, base.Owner.Monster.RunRng.MonsterAi);
				CreatureCmd.Stun(base.Owner, StunnedMove, nextState);
				((ThievingHopper)base.Owner.Monster).IsHovering = false;
			}
		}
	}

	private void StunnedMove(IReadOnlyList<Creature> targets)
	{
		return;
	}
}
