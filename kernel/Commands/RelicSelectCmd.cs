using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;

namespace MegaCrit.Sts2.Core.Commands;

public static class RelicSelectCmd
{
	public static RelicModel? FromChooseARelicScreen(Player player, IReadOnlyList<RelicModel> relics)
	{
		return relics.FirstOrDefault();
	}
}
