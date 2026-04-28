using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class BadLuck : CardModel
{
	public override bool CanBeGeneratedByModifiers => false;

	public override int MaxUpgradeLevel => 0;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new global::_003C_003Ez__ReadOnlyArray<CardKeyword>(new CardKeyword[2]
	{
		CardKeyword.Eternal,
		CardKeyword.Unplayable
	});

	public override bool HasTurnEndInHandEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new HpLossVar(13m));

	public BadLuck()
		: base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
	{
	}

	public override void OnTurnEndInHand(PlayerChoiceContext? choiceContext)
	{
		PlayerChoiceContext context = choiceContext ?? new ThrowingPlayerChoiceContext();
		CreatureCmd.Damage(context, base.Owner.Creature, base.DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
	}
}
