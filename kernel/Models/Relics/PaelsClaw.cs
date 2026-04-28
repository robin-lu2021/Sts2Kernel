using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Enchantments;
namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class PaelsClaw : RelicModel
{
	public const int cardsCount = 3;

	private const int _enchantmentAmount = 2;

	public override RelicRarity Rarity => RelicRarity.Ancient;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new CardsVar(3),
		new StringVar("EnchantmentName", ModelDb.Enchantment<Goopy>().Title.GetFormattedText())
	});


	public override void AfterObtained()
	{
		IEnumerable<CardModel> enumerable = PileType.Deck.GetPile(base.Owner).Cards.ToList();
		foreach (CardModel item in enumerable)
		{
			if (ModelDb.Enchantment<Goopy>().CanEnchant(item))
			{
				CardCmd.Enchant<Goopy>(item, 1m);
			}
		}
		return;
	}
}