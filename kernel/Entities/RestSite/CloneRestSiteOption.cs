using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace MegaCrit.Sts2.Core.Entities.RestSite;

public class CloneRestSiteOption : RestSiteOption
{
	public override string OptionId => "CLONE";

	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			description.Add("EnchantmentName", ModelDb.Enchantment<Clone>().Title.GetFormattedText());
			return description;
		}
	}

	public CloneRestSiteOption(Player owner)
		: base(owner)
	{
	}

	public override bool OnSelect()
	{
		IEnumerable<CardModel> enumerable = base.Owner.Deck.Cards.Where((CardModel c) => c.Enchantment is Clone).ToList();
		foreach (CardModel item in enumerable)
		{
			CardModel card = base.Owner.RunState.CloneCard(item);
			CardPileCmd.Add(card, PileType.Deck);
		}
		return true;
	}
}
