using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class LightningRodPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override void AfterEnergyReset(Player player)
	{
		if (player == base.Owner.Player)
		{
			OrbCmd.Channel<LightningOrb>(new ThrowingPlayerChoiceContext(), base.Owner.Player);
			PowerCmd.Decrement(this);
		}
	}
}
