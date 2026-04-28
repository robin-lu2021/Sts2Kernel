using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Powers;

public class MonarchsGazeStrengthDownPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => KernelModelDb.Card<MonarchsGaze>();

	protected override bool IsPositive => false;
}

