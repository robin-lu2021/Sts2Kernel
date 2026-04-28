using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class SummonForth : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new ForgeVar(8));


	public SummonForth()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ForgeCmd.Forge(base.DynamicVars.Forge.IntValue, base.Owner, this);
		List<CardModel> enumerable = base.Owner.PlayerCombatState.AllCards.Where((CardModel c) => c is SovereignBlade && c.Pile?.Type != PileType.Hand).ToList();
		foreach (CardModel item in enumerable)
		{
			CardPileCmd.Add(item, PileType.Hand);
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Forge.UpgradeValueBy(3m);
	}
}
