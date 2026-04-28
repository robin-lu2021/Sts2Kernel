using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class InfernoPower : PowerModel
{
	private const string _selfDamageKey = "SelfDamage";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DamageVar("SelfDamage", 0m, ValueProp.Unblockable | ValueProp.Unpowered));

	public override void AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == base.Owner.Player)
		{
			DamageVar damageVar = (DamageVar)base.DynamicVars["SelfDamage"];
			CreatureCmd.Damage(choiceContext, base.Owner, damageVar.BaseValue, damageVar.Props, base.Owner, null);
		}
	}

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != base.Owner || result.UnblockedDamage <= 0 || base.Owner.CombatState.CurrentSide != base.Owner.Side)
		{
			return;
		}
		foreach (Creature hittableEnemy in base.CombatState.HittableEnemies)
		{
			NFireBurstVfx child = NFireBurstVfx.Create(hittableEnemy, 0.75f);
		}
		CreatureCmd.Damage(choiceContext, base.CombatState.HittableEnemies, base.Amount, ValueProp.Unpowered, base.Owner, null);
	}

	public void IncrementSelfDamage()
	{
		AssertMutable();
		base.DynamicVars["SelfDamage"].BaseValue++;
	}
}
