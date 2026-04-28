using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Potions;

public sealed class FoulPotion : global::MegaCrit.Sts2.Core.PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Event;

	public override PotionUsage Usage => PotionUsage.AnyTime;

	public override TargetType TargetType
	{
		get
		{
			if (!CombatManager.Instance.IsInProgress)
			{
				return TargetType.TargetedNoCreature;
			}
			return TargetType.AllEnemies;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DamageVar(12m, ValueProp.Unpowered),
		new GoldVar(100)
	});

	public override bool PassesCustomUsabilityCheck
	{
		get
		{
			if (CombatManager.Instance.IsInProgress)
			{
				return true;
			}
			if (base.Owner.RunState.CurrentRoom is MerchantRoom)
			{
				return true;
			}
			if (base.Owner.RunState.CurrentRoom is EventRoom eventRoom && eventRoom.CanonicalEvent is FakeMerchant)
			{
				return true;
			}
			return false;
		}
	}

	protected override void OnUse(PlayerChoiceContext? choiceContext, Creature? target)
	{
		if (CombatManager.Instance.IsInProgress)
		{
			Creature creature = base.Owner.Creature;
			DamageVar damage = base.DynamicVars.Damage;
			CreatureCmd.Damage(choiceContext, base.Owner.Creature.CombatState.Creatures.Where((Creature c) => !c.IsPet), damage.BaseValue, damage.Props, creature, null);
		}
		else if (base.Owner.RunState.CurrentRoom is MerchantRoom)
		{
			PlayerCmd.GainGold(base.DynamicVars.Gold.BaseValue, base.Owner);
		}
		else
		{
			if (!(base.Owner.RunState.CurrentRoom is EventRoom eventRoom) || !(eventRoom.CanonicalEvent is FakeMerchant))
			{
				return;
			}
			foreach (Player player in base.Owner.RunState.Players)
			{
				FakeMerchant fakeMerchant = (FakeMerchant)RunManager.Instance.EventSynchronizer.GetEventForPlayer(player);
				fakeMerchant.FoulPotionThrown(this);
			}
		}
	}
}
