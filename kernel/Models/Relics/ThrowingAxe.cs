using MegaCrit.Sts2.Core;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class ThrowingAxe : RelicModel
{
	private bool _usedThisCombat;

	public override RelicRarity Rarity => RelicRarity.Ancient;

	private bool UsedThisCombat
	{
		get
		{
			return _usedThisCombat;
		}
		set
		{
			AssertMutable();
			_usedThisCombat = value;
		}
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (!(room is CombatRoom))
		{
			return;
		}
		UsedThisCombat = false;
		base.Status = RelicStatus.Active;
		return;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (UsedThisCombat)
		{
			return playCount;
		}
		if (card.Owner != base.Owner)
		{
			return playCount;
		}
		return playCount + 1;
	}

	public override void AfterModifyingCardPlayCount(CardModel card)
	{
		UsedThisCombat = true;
		 
		base.Status = RelicStatus.Normal;
		return;
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		UsedThisCombat = false;
		base.Status = RelicStatus.Normal;
		return;
	}
}