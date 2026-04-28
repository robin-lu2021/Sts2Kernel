using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core.Entities.RestSite;

public sealed class SmithRestSiteOption : RestSiteOption
{
	private IEnumerable<CardModel>? _selection;

	public override string OptionId => "SMITH";

	public override IEnumerable<string> AssetPaths => base.AssetPaths;

	public int SmithCount { get; set; } = 1;

	public override LocString Description
	{
		get
		{
			LocString locString;
			if (base.IsEnabled)
			{
				locString = new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");
				locString.Add("Count", SmithCount);
			}
			else
			{
				locString = new LocString("rest_site_ui", "OPTION_" + OptionId + ".descriptionDisabled");
			}
			return locString;
		}
	}

	public SmithRestSiteOption(Player owner)
		: base(owner)
	{
		Log.Info("Set enabled");
		base.IsEnabled = owner.Deck.UpgradableCardCount != 0;
	}

	public override bool OnSelect()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, SmithCount)
		{
			Cancelable = true,
			RequireManualConfirmation = true
		};
		_selection = CardSelectCmd.FromDeckForUpgrade(base.Owner, prefs);
		if (!_selection.Any())
		{
			return false;
		}
		foreach (CardModel item in _selection)
		{
			CardCmd.Upgrade(item, CardPreviewStyle.None);
		}
		Hook.AfterRestSiteSmith(base.Owner.RunState, base.Owner);
		return true;
	}
}
