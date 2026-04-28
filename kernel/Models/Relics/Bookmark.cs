using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class Bookmark : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Rare;


	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		List<CardModel> list = (from c in CardPile.GetCards(base.Owner, PileType.Hand)
			where c.ShouldRetainThisTurn && c.EnergyCost.GetWithModifiers(CostModifiers.Local) > 0 && !c.EnergyCost.CostsX
			select c).ToList();
		if (list.Count == 0)
		{
			return;
		}
		 
		Rng combatCardSelection = base.Owner.RunState.Rng.CombatCardSelection;
		CardModel cardModel = combatCardSelection.NextItem(list);
		cardModel.EnergyCost.AddUntilPlayed(-1);
		return;
	}
}