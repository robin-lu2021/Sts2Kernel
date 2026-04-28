using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class CentennialPuzzle : RelicModel
{
	private bool _usedThisCombat;

	public override RelicRarity Rarity => RelicRarity.Common;


	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new CardsVar(3));

	public bool UsedThisCombat
	{
		get
		{
			return _usedThisCombat;
		}
		private set
		{
			if (_usedThisCombat != value)
			{
				AssertMutable();
				_usedThisCombat = value;
			}
		}
	}

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (CombatManager.Instance.IsInProgress && target == base.Owner.Creature && result.UnblockedDamage > 0 && !UsedThisCombat)
		{
			 
			UsedThisCombat = true;
			for (int i = 0; (decimal)i < base.DynamicVars.Cards.BaseValue; i++)
			{
				CardPileCmd.Draw(choiceContext, base.Owner);
			}
		}
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		UsedThisCombat = false;
		return;
	}
}