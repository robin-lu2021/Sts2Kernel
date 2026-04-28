using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Regret : CardModel
{
	private int _cardsInHand;

	public override int MaxUpgradeLevel => 0;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new global::_003C_003Ez__ReadOnlySingleElementList<CardKeyword>(CardKeyword.Unplayable);

	private int CardsInHand
	{
		get
		{
			return _cardsInHand;
		}
		set
		{
			AssertMutable();
			_cardsInHand = value;
		}
	}

	public override bool HasTurnEndInHandEffect => true;

	public Regret()
		: base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
	{
	}

	public override void BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != CombatSide.Player)
		{
			return;
		}
		if (base.Pile.Type != PileType.Hand)
		{
			return;
		}
		CardsInHand = base.Pile.Cards.Count;
		return;
	}

	public override void OnTurnEndInHand(PlayerChoiceContext? choiceContext)
	{
		PlayerChoiceContext context = choiceContext ?? new ThrowingPlayerChoiceContext();
		CreatureCmd.Damage(context, base.Owner.Creature, CardsInHand, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
		CardsInHand = 0;
	}
}
