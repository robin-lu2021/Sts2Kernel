using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class LeesWaffle : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Shop;

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new MaxHpVar(7m));

	public override void AfterObtained()
	{
		Creature creature = base.Owner.Creature;
		CreatureCmd.GainMaxHp(creature, base.DynamicVars.MaxHp.BaseValue);
		CreatureCmd.Heal(creature, creature.MaxHp - creature.CurrentHp);
	}
}