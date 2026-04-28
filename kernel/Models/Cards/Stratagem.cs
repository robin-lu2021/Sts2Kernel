using MegaCrit.Sts2.Core.Entities.Cards;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Stratagem : CardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.SingleplayerOnly;

	public Stratagem()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

}
