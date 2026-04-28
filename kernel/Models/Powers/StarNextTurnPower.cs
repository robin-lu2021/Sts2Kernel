using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class StarNextTurnPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override void AfterEnergyReset(Player player)
	{
		if (player == base.Owner.Player)
		{
			PlayerCmd.GainStars(base.Amount, player);
			PowerCmd.Remove(this);
		}
	}
}
