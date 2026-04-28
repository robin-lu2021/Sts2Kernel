using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Powers;

public class PiercingWailPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => KernelModelDb.Card<PiercingWail>();

	protected override bool IsPositive => false;
}

