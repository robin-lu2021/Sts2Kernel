using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public class MysteriousKnight : FlailKnight
{
	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<StrengthPower>(base.Creature, 6m, base.Creature, null);
		PowerCmd.Apply<PlatingPower>(base.Creature, 6m, base.Creature, null);
	}
}
