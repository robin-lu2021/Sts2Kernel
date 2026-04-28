using MegaCrit.Sts2.Core.Entities.Cards;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class WellLaidPlans : CardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.SingleplayerOnly;

	public WellLaidPlans()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

}
