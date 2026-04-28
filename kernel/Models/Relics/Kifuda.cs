using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class Kifuda : RelicModel
{
	private const int _adroitAmount = 3;

	public override RelicRarity Rarity => RelicRarity.Shop;

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new CardsVar(3));


	public override void AfterObtained()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, base.DynamicVars.Cards.IntValue)
		{
			Cancelable = false,
			RequireManualConfirmation = true
		};
		Adroit canonicalEnchantment = ModelDb.Enchantment<Adroit>();
		foreach (CardModel item in CardSelectCmd.FromDeckForEnchantment(base.Owner, canonicalEnchantment, 3, prefs))
		{
			CardCmd.Enchant(canonicalEnchantment.ToMutable(), item, 3m);
			CardCmd.Preview(item);
		}
	}
}