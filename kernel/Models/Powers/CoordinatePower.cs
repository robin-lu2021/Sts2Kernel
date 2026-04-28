using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Powers;

public class CoordinatePower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => KernelModelDb.Card<Coordinate>();
}

