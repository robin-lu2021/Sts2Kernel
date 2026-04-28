using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MegaCrit.Sts2.Core.Entities.RestSite;

public class LiftRestSiteOption : RestSiteOption
{
	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			Girya relic = base.Owner.GetRelic<Girya>();
			int num = 3 - relic.TimesLifted;
			description.Add("LiftsLeft", num);
			return description;
		}
	}

	public override string OptionId => "LIFT";

	public LiftRestSiteOption(Player owner)
		: base(owner)
	{
	}

	public override bool OnSelect()
	{
		base.Owner.GetRelic<Girya>().TimesLifted++;
		return true;
	}
}
