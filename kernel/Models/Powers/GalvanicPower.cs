using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class GalvanicPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new StringVar("AfflictionTitle", ModelDb.Affliction<Galvanized>().Title.GetFormattedText()));


	public override void BeforeCombatStart()
	{
		foreach (Creature item in base.Owner.CombatState.Allies.ToList())
		{
			if (!item.IsPlayer)
			{
				continue;
			}
			IEnumerable<CardModel> enumerable = item.Player.PlayerCombatState.AllCards.Where((CardModel c) => c.Type == CardType.Power);
			foreach (CardModel item2 in enumerable)
			{
				CardCmd.Afflict<Galvanized>(item2, base.Amount);
			}
		}
	}

	public override void AfterCardEnteredCombat(CardModel card)
	{
		if (card.Affliction == null && card.Type == CardType.Power)
		{
			CardCmd.Afflict<Galvanized>(card, base.Amount);
		}
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Affliction is Galvanized)
		{
			CreatureCmd.Damage(context, cardPlay.Card.Owner.Creature, base.Amount, ValueProp.Unpowered | ValueProp.Move, null, null);
		}
	}
}
