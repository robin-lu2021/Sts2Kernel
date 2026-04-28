using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Modifiers;

public class Murderous : ModifierModel
{
	private const int _strengthAmount = 3;

	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (room is CombatRoom combatRoom)
		{
			PowerCmd.Apply<StrengthPower>(combatRoom.CombatState.Creatures, 3m, null, null);
		}
	}

	public override void AfterCreatureAddedToCombat(Creature creature)
	{
		if (creature.Side == CombatSide.Player)
		{
			return;
		}
		PowerCmd.Apply<StrengthPower>(creature, 3m, null, null);
	}
}
