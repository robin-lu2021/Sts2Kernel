using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class CrimsonMantle : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new PowerVar<CrimsonMantlePower>(8m));

	public CrimsonMantle()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CrimsonMantlePower? power = PowerCmd.Apply<CrimsonMantlePower>(base.Owner.Creature, base.DynamicVars["CrimsonMantlePower"].BaseValue, base.Owner.Creature, this);
		power?.IncrementSelfDamage();
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars["CrimsonMantlePower"].UpgradeValueBy(2m);
	}
}
