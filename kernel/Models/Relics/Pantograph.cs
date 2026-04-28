using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class Pantograph : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new HealVar(25m));

	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (!base.Owner.Creature.IsDead)
		{
			bool flag = base.Owner.RunState.Map.BossMapPoint.parents.Contains(base.Owner.RunState.CurrentMapPoint);
			base.Status = (flag ? RelicStatus.Active : RelicStatus.Normal);
			if (room.RoomType == RoomType.Boss)
			{
				 
				CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars.Heal.BaseValue);
			}
		}
	}
}