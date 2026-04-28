using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Toxic : CardModel
{
	public override int MaxUpgradeLevel => 0;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new global::_003C_003Ez__ReadOnlySingleElementList<CardKeyword>(CardKeyword.Exhaust);

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DamageVar(5m, ValueProp.Unpowered | ValueProp.Move));

	public override bool HasTurnEndInHandEffect => true;

	public Toxic()
		: base(1, CardType.Status, CardRarity.Status, TargetType.None)
	{
	}

	public override void OnTurnEndInHand(PlayerChoiceContext? choiceContext)
	{
		PlayerChoiceContext context = choiceContext ?? new ThrowingPlayerChoiceContext();
		CreatureCmd.Damage(context, base.Owner.Creature, base.DynamicVars.Damage, this);
	}
}
