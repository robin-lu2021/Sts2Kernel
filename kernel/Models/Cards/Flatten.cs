using MegaCrit.Sts2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Flatten : CardModel
{
	protected override bool ShouldGlowGoldInternal => HasOstyAttackedThisTurn;

	protected override bool ShouldGlowRedInternal => base.Owner.IsOstyMissing;

	protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.OstyAttack };

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new OstyDamageVar(12m, ValueProp.Move));

	private bool HasOstyAttackedThisTurn => CombatManager.Instance.History.Entries.OfType<CreatureAttackedEntry>().Any((CreatureAttackedEntry e) => e.Actor == base.Owner.Osty && e.HappenedThisTurn(base.CombatState));

	public Flatten()
		: base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		if (!Osty.CheckMissingWithAnim(base.Owner))
		{
			DamageCmd.Attack(base.DynamicVars.OstyDamage.BaseValue).FromOsty(base.Owner.Osty, this).Targeting(cardPlay.Target)
				
				.Execute(choiceContext);
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.OstyDamage.UpgradeValueBy(4m);
	}

	public override void AfterCardEnteredCombat(CardModel card)
	{
		if (card != this)
		{
			return;
		}
		if (!HasOstyAttackedThisTurn)
		{
			return;
		}
		ReduceCost();
		return;
	}

	public override void AfterAttack(AttackCommand command)
	{
		if (command.Attacker == null)
		{
			return;
		}
		if (command.Attacker != base.Owner.Osty)
		{
			return;
		}
		ReduceCost();
		return;
	}

	private void ReduceCost()
	{
		base.EnergyCost.SetThisTurn(0);
	}
}
