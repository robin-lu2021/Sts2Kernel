using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class FakeSneckoEye : RelicModel
{
	private int _testEnergyCostOverride = -1;

	public override RelicRarity Rarity => RelicRarity.Event;

	public override int MerchantCost => 50;


	public override void AfterObtained()
	{
		if (CombatManager.Instance.IsInProgress)
		{
			ApplyPower();
		}
	}

	public override void BeforeCombatStart()
	{
		ApplyPower();
	}

	private void ApplyPower()
	{
		PowerCmd.Apply<ConfusedPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
		ApplyTestEnergyCostOverrideToPower();
	}

	public void SetTestEnergyCostOverride(int value)
	{
		TestMode.AssertOn();
		AssertMutable();
		_testEnergyCostOverride = value;
		ApplyTestEnergyCostOverrideToPower();
	}

	private void ApplyTestEnergyCostOverrideToPower()
	{
		if (_testEnergyCostOverride >= 0)
		{
			ConfusedPower power = base.Owner.Creature.GetPower<ConfusedPower>();
			if (power != null)
			{
				power.TestEnergyCostOverride = _testEnergyCostOverride;
			}
		}
	}
}