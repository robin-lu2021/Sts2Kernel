using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BigMushroom : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Event;

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new MaxHpVar(20m),
		new CardsVar(2)
	});

	public override void AfterObtained()
	{
		CreatureCmd.GainMaxHp(base.Owner.Creature, base.DynamicVars.MaxHp.BaseValue);
		Grow();
	}

	public override void AfterRoomEntered(AbstractRoom _)
	{
		Grow();
		return;
	}

	public override decimal ModifyHandDraw(Player player, decimal cardsToDraw)
	{
		if (player != base.Owner)
		{
			return cardsToDraw;
		}
		if (player.Creature.CombatState.RoundNumber != 1)
		{
			return cardsToDraw;
		}
		return cardsToDraw - (decimal)base.DynamicVars.Cards.IntValue;
	}

	private void Grow()
	{
		NCombatRoom.Instance?.GetCreatureNode(base.Owner.Creature)?.ScaleTo(1.5f, 0f);
	}
}