using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Potions;

public sealed class BloodPotion : global::MegaCrit.Sts2.Core.PotionModel
{
	private const string _healPercentKey = "HealPercent";

	public override PotionRarity Rarity => PotionRarity.Common;

	public override PotionUsage Usage => PotionUsage.AnyTime;

	public override TargetType TargetType => TargetType.AnyPlayer;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("HealPercent", 20m));

	protected override void OnUse(PlayerChoiceContext? choiceContext, Creature? target)
	{
		global::MegaCrit.Sts2.Core.PotionModel.AssertValidForTargetedPotion(target);
		CreatureCmd.Heal(target, (decimal)target.MaxHp * base.DynamicVars["HealPercent"].BaseValue / 100m);
	}
}
