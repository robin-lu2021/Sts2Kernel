using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class ElectricShrymp : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Ancient;

	public override bool HasUponPickupEffect => true;


	public override void AfterObtained()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
		Imbued canonicalMomentum = ModelDb.Enchantment<Imbued>();
		foreach (CardModel item in CardSelectCmd.FromDeckForEnchantment(base.Owner, canonicalMomentum, 1, prefs))
		{
			CardCmd.Enchant(canonicalMomentum.ToMutable(), item, 1m);
			CardCmd.Preview(item);
		}
	}
}