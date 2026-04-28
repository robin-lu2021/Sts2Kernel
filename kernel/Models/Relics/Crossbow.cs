using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class Crossbow : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Ancient;

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		IReadOnlyList<CardModel> readOnlyList = (from c in base.Owner.Character.CardPool.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint)
			where c.Type == CardType.Attack
			select c).ToList();
		if (readOnlyList.Count == 0)
		{
			return;
		}
		 
		List<CardModel> list = KernelCardFactoryExtensions.GetDistinctForCombat(base.Owner, readOnlyList, 1, base.Owner.RunState.Rng.CombatCardGeneration).ToList();
		foreach (CardModel item in list)
		{
			item.SetToFreeThisTurn();
		}
		CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Hand, addedByPlayer: true);
	}
}
