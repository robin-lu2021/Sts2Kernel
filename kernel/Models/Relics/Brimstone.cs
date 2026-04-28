using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class Brimstone : RelicModel
{
	private const string _selfStrengthKey = "SelfStrength";

	private const string _enemyStrengthKey = "EnemyStrength";

	public override RelicRarity Rarity => RelicRarity.Shop;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new PowerVar<StrengthPower>("SelfStrength", 2m),
		new PowerVar<StrengthPower>("EnemyStrength", 1m)
	});


	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side == base.Owner.Creature.Side)
		{
			 
			PowerCmd.Apply<StrengthPower>(base.Owner.Creature, base.DynamicVars["SelfStrength"].BaseValue, base.Owner.Creature, null);
			IEnumerable<Creature> targets = from c in combatState.GetOpponentsOf(base.Owner.Creature)
				where c.IsAlive
				select c;
			PowerCmd.Apply<StrengthPower>(targets, base.DynamicVars["EnemyStrength"].BaseValue, null, null);
		}
	}
}