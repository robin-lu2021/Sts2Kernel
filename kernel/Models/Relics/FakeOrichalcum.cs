using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class FakeOrichalcum : RelicModel
{
	private bool _shouldTrigger;

	public override RelicRarity Rarity => RelicRarity.Event;

	public override int MerchantCost => 50;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new BlockVar(3m, ValueProp.Unpowered));


	private bool ShouldTrigger
	{
		get
		{
			return _shouldTrigger;
		}
		set
		{
			AssertMutable();
			_shouldTrigger = value;
		}
	}

	public override void BeforeTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		if (base.Owner.Creature.Block > 0)
		{
			return;
		}
		ShouldTrigger = true;
		return;
	}

	public override void BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (ShouldTrigger)
		{
			ShouldTrigger = false;
			 
			CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, null);
		}
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		ShouldTrigger = false;
		return;
	}
}