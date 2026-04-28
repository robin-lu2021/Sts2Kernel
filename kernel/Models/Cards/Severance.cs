using MegaCrit.Sts2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Severance : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DamageVar(13m, ValueProp.Move));


	public Severance()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
			
			.Execute(choiceContext);
		List<CardModel> souls = Soul.Create(base.Owner, 3, base.CombatState).ToList();
		CardPileAddResult drawResult = CardPileCmd.AddGeneratedCardToCombat(souls[0], PileType.Draw, addedByPlayer: true, CardPilePosition.Random);
		CardPileAddResult discardResult = CardPileCmd.AddGeneratedCardToCombat(souls[1], PileType.Discard, addedByPlayer: true);
		CardPileCmd.AddGeneratedCardToCombat(souls[2], PileType.Hand, addedByPlayer: true);
		CardCmd.PreviewCardPileAdd(new global::_003C_003Ez__ReadOnlyArray<CardPileAddResult>(new CardPileAddResult[2] { drawResult, discardResult }));
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(5m);
	}
}
