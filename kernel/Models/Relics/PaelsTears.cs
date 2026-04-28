using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class PaelsTears : RelicModel
{
	private bool _hadLeftoverEnergy;

	public override RelicRarity Rarity => RelicRarity.Ancient;

	private bool HadLeftoverEnergy
	{
		get
		{
			return _hadLeftoverEnergy;
		}
		set
		{
			AssertMutable();
			_hadLeftoverEnergy = value;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new EnergyVar(2));


	public override void BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != CombatSide.Player)
		{
			return;
		}
		HadLeftoverEnergy = base.Owner.PlayerCombatState.Energy > 0;
		return;
	}

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side == base.Owner.Creature.Side && HadLeftoverEnergy)
		{
			 
			PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
		}
	}

	public override void AfterCombatEnd(CombatRoom room)
	{
		HadLeftoverEnergy = false;
		return;
	}
}