using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Orbs;

public class PlasmaOrb : OrbModel
{
	protected override string ChannelSfx => "event:/sfx/characters/defect/defect_plasma_channel";

	public override decimal PassiveVal => 1m;

	public override decimal EvokeVal => 2m;

	public override Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
	{
		return Passive(choiceContext, null);
	}

	public override Task Passive(PlayerChoiceContext choiceContext, Creature? target)
	{
		if (target != null)
		{
			throw new InvalidOperationException("Plasma orbs cannot target creatures.");
		}
		Trigger();
		PlayerCmd.GainEnergy(PassiveVal, base.Owner);
		return Task.CompletedTask;
	}

	public override Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
	{
		PlayEvokeSfx();
		PlayerCmd.GainEnergy(EvokeVal, base.Owner);
		return Task.FromResult<IEnumerable<Creature>>(new global::_003C_003Ez__ReadOnlySingleElementList<Creature>(base.Owner.Creature));
	}
}
