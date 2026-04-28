using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Potions;

public sealed class FairyInABottle : global::MegaCrit.Sts2.Core.PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Rare;

	public override PotionUsage Usage => PotionUsage.Automatic;

	public override TargetType TargetType => TargetType.Self;

	public override bool CanBeGeneratedInCombat => false;

	protected override void OnUse(PlayerChoiceContext? choiceContext, Creature? target)
	{
		global::MegaCrit.Sts2.Core.PotionModel.AssertValidForTargetedPotion(target);
		CreatureCmd.Heal(target, Math.Max((decimal)target.MaxHp * 0.3m, 1m));
	}

	public override bool ShouldDie(Creature creature)
	{
		if (creature != base.Owner.Creature)
		{
			return true;
		}
		return false;
	}

	public override void AfterPreventingDeath(Creature creature)
	{
		Use(new ThrowingPlayerChoiceContext(), creature);
	}
}
