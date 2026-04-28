using MegaCrit.Sts2.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class MrStruggles : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Event;

	public override void AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == base.Owner)
		{
			 
			CombatState combatState = player.Creature.CombatState;
			CreatureCmd.Damage(choiceContext, combatState.HittableEnemies, combatState.RoundNumber, ValueProp.Unpowered, base.Owner.Creature);
		}
	}
}