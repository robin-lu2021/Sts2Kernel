using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class StoneCalendar : RelicModel
{
	private const string _damageTurnKey = "DamageTurn";

	private bool _isActivating;

	public override RelicRarity Rarity => RelicRarity.Rare;



	private bool IsActivating
	{
		get
		{
			return _isActivating;
		}
		set
		{
			AssertMutable();
			_isActivating = value;
			InvokeDisplayAmountChanged();
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DamageVar(52m, ValueProp.Unpowered),
		new DynamicVar("DamageTurn", 7m)
	});

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		if (combatState.RoundNumber == base.DynamicVars["DamageTurn"].IntValue)
		{
			base.Status = RelicStatus.Active;
		}
		InvokeDisplayAmountChanged();
		return;
	}

	public override void BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Creature.Side)
		{
			int intValue = base.DynamicVars["DamageTurn"].IntValue;
			int roundNumber = base.Owner.Creature.CombatState.RoundNumber;
			base.Status = RelicStatus.Normal;
			if (roundNumber == intValue)
			{
				DoActivateVisuals();
				CreatureCmd.Damage(choiceContext, base.Owner.Creature.CombatState.HittableEnemies, base.DynamicVars.Damage, base.Owner.Creature);
				InvokeDisplayAmountChanged();
			}
		}
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		base.Status = RelicStatus.Normal;
		InvokeDisplayAmountChanged();
		return;
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (!(room is CombatRoom))
		{
			return;
		}
		base.Status = RelicStatus.Normal;
		InvokeDisplayAmountChanged();
		return;
	}

	private void DoActivateVisuals()
	{
		IsActivating = true;
		 
		IsActivating = false;
	}
}
