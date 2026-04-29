using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class WellLaidPlans : CardModel
{
	private const string _retainAmount = "RetainAmount";

	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.SingleplayerOnly;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("RetainAmount", 1m));

	public WellLaidPlans()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		PowerCmd.Apply<WellLaidPlansPower>(base.Owner.Creature, base.DynamicVars["RetainAmount"].BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars["RetainAmount"].UpgradeValueBy(1m);
	}
}
