using MegaCrit.Sts2.Core.Entities.Cards;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class BeaconOfHope : CardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

	public BeaconOfHope()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

}
