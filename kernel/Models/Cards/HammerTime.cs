using MegaCrit.Sts2.Core.Entities.Cards;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class HammerTime : CardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

	public HammerTime()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

}
