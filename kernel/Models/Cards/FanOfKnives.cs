using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class FanOfKnives : CardModel
{
	private const string _shivsKey = "Shivs";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new CardsVar("Shivs", 4));

	public FanOfKnives()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		PowerCmd.Apply<FanOfKnivesPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);
		for (int i = 0; i < base.DynamicVars["Shivs"].IntValue; i++)
		{
			Shiv.CreateInHand(base.Owner, base.CombatState);
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars["Shivs"].UpgradeValueBy(1m);
	}
}
