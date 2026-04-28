using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class ChemicalX : RelicModel
{
	private const string _increaseKey = "Increase";

	public override RelicRarity Rarity => RelicRarity.Shop;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("Increase", 2m));

	public override void BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner)
		{
			return;
		}
		if (!cardPlay.Card.EnergyCost.CostsX && !cardPlay.Card.HasStarCostX)
		{
			return;
		}
		 
		return;
	}

	public override int ModifyXValue(CardModel card, int originalValue)
	{
		if (base.Owner != card.Owner)
		{
			return originalValue;
		}
		return originalValue + base.DynamicVars["Increase"].IntValue;
	}
}