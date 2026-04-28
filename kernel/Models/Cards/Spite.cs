using MegaCrit.Sts2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Spite : CardModel
{
	protected override bool ShouldGlowGoldInternal => LostHpThisTurn(base.Owner.Creature);

	private bool TookDamageThisTurn => CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>().Any((DamageReceivedEntry e) => e.HappenedThisTurn(base.CombatState) && e.Receiver == base.Owner.Creature && e.Result.UnblockedDamage > 0 && e.CurrentSide == CombatSide.Player);

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DamageVar(5m, ValueProp.Move),
		new RepeatVar(2)
	});

	public Spite()
		: base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		int hitCount = ((!LostHpThisTurn(base.Owner.Creature)) ? 1 : base.DynamicVars.Repeat.IntValue);
		DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).WithHitCount(hitCount).FromCard(this)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);
		if (TookDamageThisTurn)
		{
			CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.IntValue, base.Owner);
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Repeat.UpgradeValueBy(1m);
	}

	private static bool LostHpThisTurn(Creature creature)
	{
		return CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>().Any((DamageReceivedEntry e) => e.HappenedThisTurn(creature.CombatState) && e.Receiver == creature && e.Result.UnblockedDamage > 0);
	}
}
