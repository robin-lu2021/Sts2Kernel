using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Turbo : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new EnergyVar(2));

	public Turbo()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
		CardModel card = base.CombatState.CreateCard<Void>(base.Owner);
		CardCmd.PreviewCardPileAdd(CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, addedByPlayer: true));
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Energy.UpgradeValueBy(1m);
	}
}
