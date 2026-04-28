using MegaCrit.Sts2.Core;
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Malaise : CardModel
{
	public override TargetType TargetType => TargetType.AnyEnemy;

	protected override bool HasEnergyCostX => true;

	public override IEnumerable<CardKeyword> CanonicalKeywords => new global::_003C_003Ez__ReadOnlySingleElementList<CardKeyword>(CardKeyword.Exhaust);

	public Malaise()
		: base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		int powerAmount = ResolveEnergyXValue();
		if (base.IsUpgraded)
		{
			powerAmount++;
		}
		PowerCmd.Apply<StrengthPower>(cardPlay.Target, -powerAmount, base.Owner.Creature, this);
		PowerCmd.Apply<WeakPower>(cardPlay.Target, powerAmount, base.Owner.Creature, this);
	}
}
