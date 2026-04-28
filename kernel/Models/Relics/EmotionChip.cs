using MegaCrit.Sts2.Core;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class EmotionChip : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	private bool LostHpInPreviousTurn => CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>().Any((DamageReceivedEntry e) => e.Receiver == base.Owner.Creature && !e.Result.WasFullyBlocked && e.RoundNumber + 1 == base.Owner.Creature.CombatState.RoundNumber);

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			return;
		}
		if (target != base.Owner.Creature)
		{
			return;
		}
		if (result.UnblockedDamage <= 0)
		{
			return;
		}
		base.Status = RelicStatus.Active;
		 
		return;
	}

	public override void AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner)
		{
			return;
		}
		base.Status = RelicStatus.Normal;
		if (!LostHpInPreviousTurn)
		{
			return;
		}
		 
		foreach (OrbModel orb in base.Owner.PlayerCombatState.OrbQueue.Orbs)
		{
			OrbCmd.Passive(choiceContext, orb, null);
		}
	}

	public override void AfterCombatEnd(CombatRoom room)
	{
		base.Status = RelicStatus.Normal;
		return;
	}
}