using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MegaCrit.Sts2.Core.Models.Potions;

public sealed class Clarity : global::MegaCrit.Sts2.Core.PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Uncommon;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.AnyPlayer;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new PowerVar<ClarityPower>(3m),
		new CardsVar(1)
	});

	protected override void OnUse(PlayerChoiceContext? choiceContext, Creature? target)
	{
		global::MegaCrit.Sts2.Core.PotionModel.AssertValidForTargetedPotion(target);
		CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, target.Player);
		PowerCmd.Apply<ClarityPower>(target, base.DynamicVars["ClarityPower"].BaseValue, base.Owner.Creature, null);
	}
}
