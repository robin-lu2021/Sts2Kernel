using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Potions;

public sealed class BlessingOfTheForge : global::MegaCrit.Sts2.Core.PotionModel
{
	public override PotionRarity Rarity => PotionRarity.Uncommon;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.Self;

	protected override void OnUse(PlayerChoiceContext? choiceContext, Creature? target)
	{
		foreach (CardModel card in PileType.Hand.GetPile(base.Owner).Cards)
		{
			if (card.IsUpgradable)
			{
				CardCmd.Upgrade(card);
			}
		}
		return;
	}
}
