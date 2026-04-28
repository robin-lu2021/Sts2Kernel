using MegaCrit.Sts2.Core.Models.Cards.Mocks;

namespace MegaCrit.Sts2.Core.Models.Powers.Mocks;

public class MockTemporaryStrengthLossPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => KernelModelDb.Card<MockSkillCard>();

	protected override bool IsPositive => false;
}

