using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MegaCrit.Sts2.Core.Entities.RestSite;

public class HatchRestSiteOption : RestSiteOption
{
	public override string OptionId => "HATCH";

	public HatchRestSiteOption(Player owner)
		: base(owner)
	{
	}

	public override bool OnSelect()
	{
		RelicCmd.Obtain<Byrdpip>(base.Owner);
		return true;
	}
}
