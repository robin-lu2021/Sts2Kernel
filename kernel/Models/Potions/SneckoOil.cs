using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Potions;

public sealed class SneckoOil : global::MegaCrit.Sts2.Core.PotionModel
{
	private int _testEnergyCostOverride = -1;

	public override PotionRarity Rarity => PotionRarity.Rare;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.AnyPlayer;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new CardsVar(7));

	public int TestEnergyCostOverride
	{
		get
		{
			return _testEnergyCostOverride;
		}
		set
		{
			TestMode.AssertOn();
			AssertMutable();
			_testEnergyCostOverride = value;
		}
	}

	protected override void OnUse(PlayerChoiceContext? choiceContext, Creature? target)
	{
		global::MegaCrit.Sts2.Core.PotionModel.AssertValidForTargetedPotion(target);
		CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, target.Player);
		IEnumerable<CardModel> enumerable = PileType.Hand.GetPile(target.Player).Cards.Where((CardModel c) => !c.EnergyCost.CostsX);
		foreach (CardModel item in enumerable)
		{
			if (item.EnergyCost.GetWithModifiers(CostModifiers.None) >= 0)
			{
				item.EnergyCost.SetThisTurnOrUntilPlayed(NextEnergyCost());
			}
		}
	}

	private int NextEnergyCost()
	{
		if (TestEnergyCostOverride >= 0)
		{
			return TestEnergyCostOverride;
		}
		return base.Owner.RunState.Rng.CombatEnergyCosts.NextInt(4);
	}
}
