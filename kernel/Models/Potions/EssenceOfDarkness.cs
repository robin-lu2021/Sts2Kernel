using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace MegaCrit.Sts2.Core.Models.Potions;

public sealed class EssenceOfDarkness : global::MegaCrit.Sts2.Core.PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Rare;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.Self;

	protected override void OnUse(PlayerChoiceContext? choiceContext, Creature? target)
	{
		int count = base.Owner.PlayerCombatState.OrbQueue.Capacity;
		for (int i = 0; i < count; i++)
		{
			OrbCmd.Channel<DarkOrb>(choiceContext, base.Owner);
		}
	}
}
