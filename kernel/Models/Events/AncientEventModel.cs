using System;
using MegaCrit.Sts2.Core.Commands;

namespace MegaCrit.Sts2.Core.Models.Events;

public abstract class AncientEventModel : global::MegaCrit.Sts2.Core.AncientEventModel
{
	protected new EventOption RelicOption<TRelic>(string pageName = "INITIAL", string? customDonePage = null) where TRelic : RelicModel
	{
		RelicModel mutableRelic = ModelDb.Relic<TRelic>().ToMutable();
		string textKey = OptionKey(pageName, mutableRelic.Id);
		return EventOption.FromRelic(mutableRelic, this, OnChosen, textKey);

		void OnChosen()
		{
			if (Owner == null)
			{
				throw new InvalidOperationException($"Ancient '{Id}' does not have an owner.");
			}
			RelicCmd.Obtain(mutableRelic, Owner);
			_customDonePage = customDonePage;
			Done();
		}
	}

	protected new EventOption RelicOption(object relicObject, string pageName = "INITIAL", string? customDonePage = null)
	{
		RelicModel displayRelic = ConvertRelicForDisplay(relicObject);
		string textKey = OptionKey(pageName, displayRelic.Id);
		return EventOption.FromRelic(displayRelic, this, OnChosen, textKey);

		void OnChosen()
		{
			if (Owner == null)
			{
				throw new InvalidOperationException($"Ancient '{Id}' does not have an owner.");
			}
			RelicCmd.Obtain(ConvertRelicForCommand(relicObject), Owner);
			_customDonePage = customDonePage;
			Done();
		}
	}
}
