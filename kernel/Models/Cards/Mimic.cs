using MegaCrit.Sts2.Core.Entities.Cards;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Mimic : CardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

	public Mimic()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
	{
	}

}
