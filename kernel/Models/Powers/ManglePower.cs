using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Powers;

public class ManglePower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => KernelModelDb.Card<Mangle>();

	protected override bool IsPositive => false;
}

