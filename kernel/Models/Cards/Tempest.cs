using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Tempest : CardModel
{
	protected override bool HasEnergyCostX => true;

	public Tempest()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int numOfOrbs = ResolveEnergyXValue();
		if (base.IsUpgraded)
		{
			numOfOrbs++;
		}
		for (int i = 0; i < numOfOrbs; i++)
		{
			OrbCmd.Channel<LightningOrb>(choiceContext, base.Owner);
		}
	}
}
