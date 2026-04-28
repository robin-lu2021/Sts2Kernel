using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Potions;

public sealed class EntropicBrew : global::MegaCrit.Sts2.Core.PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Rare;

	public override PotionUsage Usage => PotionUsage.AnyTime;

	public override TargetType TargetType => TargetType.Self;

	protected override void OnUse(PlayerChoiceContext? choiceContext, Creature? target)
	{
		while (base.Owner.HasOpenPotionSlots)
		{
			PotionModel potion = PotionFactory.CreateRandomPotionOutOfCombat(base.Owner, base.Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
			if (!(RunSynchronously(PotionCmd.TryToProcure(potion, base.Owner).success)))
			{
				break;
			}
		}
	}
}
