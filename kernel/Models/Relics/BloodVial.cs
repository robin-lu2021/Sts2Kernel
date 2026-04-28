using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BloodVial : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Common;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new HealVar(2m));

	public override void AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == base.Owner && player.Creature.CombatState.RoundNumber <= 1)
		{
			CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars.Heal.IntValue);
		}
	}
}