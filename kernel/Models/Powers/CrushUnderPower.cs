using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Powers;

public class CrushUnderPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => KernelModelDb.Card<CrushUnder>();

	protected override bool IsPositive => false;
}

