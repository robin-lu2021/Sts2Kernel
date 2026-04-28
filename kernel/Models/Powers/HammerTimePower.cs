using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class HammerTimePower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;


	public override void AfterForge(decimal amount, Player forger, AbstractModel? source)
	{
		if (source is HammerTimePower || forger != base.Owner.Player)
		{
			return;
		}
		IEnumerable<Player> enumerable = base.CombatState.Players.Where((Player p) => p.Creature.IsAlive && p != forger);
		foreach (Player item in enumerable)
		{
			ForgeCmd.Forge(amount, item, this);
		}
	}
}
