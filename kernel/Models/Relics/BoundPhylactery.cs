using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BoundPhylactery : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Starter;

	public override bool SpawnsPets => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new SummonVar(1m));


	public override void BeforeCombatStart()
	{
		SummonPet();
	}

	public override void AfterEnergyResetLate(Player player)
	{
		if (player == base.Owner && player.Creature.CombatState.RoundNumber != 1)
		{
			SummonPet();
		}
	}

	private void SummonPet()
	{
		OstyCmd.Summon(new ThrowingPlayerChoiceContext(), base.Owner, base.DynamicVars.Summon.BaseValue, this);
	}
}