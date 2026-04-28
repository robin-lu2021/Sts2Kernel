using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class AromaOfChaos : EventModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, LetGo, "AROMA_OF_CHAOS.pages.INITIAL.options.LET_GO", HoverTipFactory.Static(StaticHoverTip.Transform)),
			new EventOption(this, MaintainControl, "AROMA_OF_CHAOS.pages.INITIAL.options.MAINTAIN_CONTROL")
		});
	}

	private void LetGo()
	{
		CardModel? cardModel = CardSelectCmd.FromDeckForTransformation(base.Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1)).FirstOrDefault();
		if (cardModel != null)
		{
			CardCmd.TransformToRandom(cardModel, base.Rng, CardPreviewStyle.None);
		}
		SetEventFinished(L10NLookup("AROMA_OF_CHAOS.pages.LET_GO.description"));
	}

	private void MaintainControl()
	{
		CardModel? cardModel = CardSelectCmd.FromDeckForUpgrade(base.Owner, new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)).FirstOrDefault();
		if (cardModel != null)
		{
			CardCmd.Upgrade(cardModel);
		}
		LocString locString = L10NLookup("AROMA_OF_CHAOS.pages.MAINTAIN_CONTROL.description");
		locString.Add("AromaPrinciple", new LocString("characters", base.Owner.Character.Id.Entry + ".aromaPrinciple"));
		SetEventFinished(locString);
	}
}
