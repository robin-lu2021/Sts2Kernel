using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace MegaCrit.Sts2.Core.MonsterMoves.Intents;

public class DeathBlowIntent : SingleAttackIntent
{
	protected override string IntentPrefix => "DEATH_BLOW";

	protected override string SpritePath => "atlases/intent_atlas.sprites/intent_death_blow.tres";

	public override IntentType IntentType => IntentType.DeathBlow;

	public override string GetAnimation(IEnumerable<Creature> targets, Creature owner)
	{
		return _cachedAnimationName ?? (_cachedAnimationName = IntentPrefix.ToLowerInvariant());
	}

	public DeathBlowIntent(Func<decimal> damageCalc)
		: base(damageCalc)
	{
	}
}
