using MegaCrit.Sts2.Core.Entities.Cards;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Ignition : CardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

	public Ignition()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
	{
	}

}
