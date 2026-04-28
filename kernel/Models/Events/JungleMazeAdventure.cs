using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class JungleMazeAdventure : EventModel
{
	private const string _soloGoldKey = "SoloGold";

	private const string _soloHpKey = "SoloHp";

	private const string _joinForcesGoldKey = "JoinForcesGold";

	public override bool IsShared => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[3]
	{
		new DynamicVar("SoloGold", 150m),
		new DamageVar("SoloHp", 18m, ValueProp.Unblockable | ValueProp.Unpowered),
		new DynamicVar("JoinForcesGold", 50m)
	});

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, DontNeedHelp, "JUNGLE_MAZE_ADVENTURE.pages.INITIAL.options.SOLO_QUEST").ThatDoesDamage(base.DynamicVars["SoloHp"].BaseValue),
			new EventOption(this, SafetyInNumbers, "JUNGLE_MAZE_ADVENTURE.pages.INITIAL.options.JOIN_FORCES")
		});
	}

	public override void CalculateVars()
	{
		base.DynamicVars["SoloGold"].BaseValue += (decimal)base.Rng.NextFloat(-15f, 15f);
		base.DynamicVars["JoinForcesGold"].BaseValue += (decimal)base.Rng.NextFloat(-15f, 15f);
	}

	private void DontNeedHelp()
	{
		CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.DynamicVars["SoloHp"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
		PlayerCmd.GainGold(base.DynamicVars["SoloGold"].BaseValue, base.Owner);
		SetEventFinished(L10NLookup("JUNGLE_MAZE_ADVENTURE.pages.SOLO_QUEST.description"));
	}

	private void SafetyInNumbers()
	{
		PlayerCmd.GainGold(base.DynamicVars["JoinForcesGold"].BaseValue, base.Owner);
		SetEventFinished(L10NLookup("JUNGLE_MAZE_ADVENTURE.pages.JOIN_FORCES.description"));
	}
}
