using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BeltBuckle : RelicModel
{
	private bool _dexterityApplied;

	public override RelicRarity Rarity => RelicRarity.Shop;

	private bool DexterityApplied
	{
		get
		{
			return _dexterityApplied;
		}
		set
		{
			AssertMutable();
			_dexterityApplied = value;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new PowerVar<DexterityPower>(2m));


	public override void AfterObtained()
	{
		if (CombatManager.Instance.IsInProgress && !base.Owner.Potions.Any())
		{
			ApplyDexterity();
		}
	}

	public override void BeforeCombatStart()
	{
		DexterityApplied = false;
		RefreshStatus();
		if (!base.Owner.Potions.Any())
		{
			ApplyDexterity();
		}
	}

	public override void AfterCombatEnd(CombatRoom room)
	{
		RefreshStatus();
		return;
	}

	public override void AfterPotionProcured(PotionModel potion)
	{
		RefreshStatus();
		if (CombatManager.Instance.IsInProgress && base.Owner.Potions.Any())
		{
			RemoveDexterity();
		}
	}

	public override void AfterPotionDiscarded(PotionModel potion)
	{
		RefreshStatus();
		if (CombatManager.Instance.IsInProgress && !base.Owner.Potions.Any())
		{
			ApplyDexterity();
		}
	}

	public override void AfterPotionUsed(PotionModel potion, Creature? target)
	{
		RefreshStatus();
		if (CombatManager.Instance.IsInProgress && !base.Owner.Potions.Any())
		{
			ApplyDexterity();
		}
	}

	public override void AfterCombatVictory(CombatRoom room)
	{
		DexterityApplied = false;
		RefreshStatus();
		return;
	}

	private void ApplyDexterity()
	{
		if (!DexterityApplied)
		{
			DexterityApplied = true;
			 
			PowerCmd.Apply<DexterityPower>(base.Owner.Creature, base.DynamicVars.Dexterity.BaseValue, null, null);
		}
	}

	private void RemoveDexterity()
	{
		if (DexterityApplied)
		{
			DexterityApplied = false;
			 
			PowerCmd.Apply<DexterityPower>(base.Owner.Creature, -base.DynamicVars.Dexterity.BaseValue, null, null);
		}
	}

	private void RefreshStatus()
	{
		if (CombatManager.Instance.IsInProgress && !base.Owner.Potions.Any())
		{
			base.Status = RelicStatus.Active;
		}
		else
		{
			base.Status = RelicStatus.Normal;
		}
	}
}