using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class SerpentForm : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new PowerVar<SerpentFormPower>(4m));

	public SerpentForm()
		: base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		PowerCmd.Apply<SerpentFormPower>(base.Owner.Creature, base.DynamicVars["SerpentFormPower"].BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars["SerpentFormPower"].UpgradeValueBy(2m);
	}
}
