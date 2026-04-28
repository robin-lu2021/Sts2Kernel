using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class FuneraryMask : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new CardsVar(3));


	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side == CombatSide.Player && combatState.RoundNumber <= 1)
		{
			 
			for (int i = 0; (decimal)i < base.DynamicVars.Cards.BaseValue; i++)
			{
				CardModel card = combatState.CreateCard(KernelModelDb.Card<Soul>(), base.Owner);
				CardCmd.PreviewCardPileAdd(CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
			}
		}
	}
}
