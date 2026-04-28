using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;

namespace MegaCrit.Sts2.Core.Entities.RestSite;

public class DigRestSiteOption : RestSiteOption
{
	public override string OptionId => "DIG";

	public DigRestSiteOption(Player owner)
		: base(owner)
	{
	}

	public override bool OnSelect()
	{
		RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(base.Owner).ToMutable(), base.Owner);
		return true;
	}
}
