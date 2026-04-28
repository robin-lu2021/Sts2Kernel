using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Darkness : CardModel
{
	public Darkness()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		OrbCmd.Channel<DarkOrb>(choiceContext, base.Owner);
		IEnumerable<OrbModel> enumerable = base.Owner.PlayerCombatState.OrbQueue.Orbs.Where((OrbModel orb) => orb is DarkOrb);
		int triggerCount = ((!base.IsUpgraded) ? 1 : 2);
		foreach (OrbModel darknessOrb in enumerable)
		{
			for (int i = 0; i < triggerCount; i++)
			{
				OrbCmd.Passive(choiceContext, darknessOrb, null);
			}
		}
	}
}
