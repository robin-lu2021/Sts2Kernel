using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class MeatOnTheBone : RelicModel
{
	private const string _hpThresholdKey = "HpThreshold";

	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DynamicVar("HpThreshold", 50m),
		new HealVar(12m)
	});

	public override void BeforeCombatStart()
	{
		if (WillHealOnCombatFinished())
		{
			base.Status = RelicStatus.Active;
		}
		return;
	}

	public override void AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (creature != base.Owner.Creature)
		{
			return;
		}
		if (!CombatManager.Instance.IsInProgress)
		{
			return;
		}
		base.Status = (WillHealOnCombatFinished() ? RelicStatus.Active : RelicStatus.Normal);
		return;
	}

	public override void AfterCombatVictoryEarly(CombatRoom _)
	{
		if (!base.Owner.Creature.IsDead)
		{
			Creature creature = base.Owner.Creature;
			if (WillHealOnCombatFinished())
			{
				base.Status = RelicStatus.Normal;
				CreatureCmd.Heal(creature, base.DynamicVars.Heal.BaseValue);
			}
		}
	}

	private bool WillHealOnCombatFinished()
	{
		Creature creature = base.Owner.Creature;
		int num = (int)((decimal)creature.MaxHp * (base.DynamicVars["HpThreshold"].BaseValue / 100m));
		return creature.CurrentHp <= num;
	}
}