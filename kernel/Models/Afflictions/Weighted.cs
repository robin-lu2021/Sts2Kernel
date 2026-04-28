using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Afflictions;

public sealed class Weighted : AfflictionModel
{
	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, Creature? target)
	{
		PlayerCmd.LoseEnergy(base.Amount, base.Card.Owner);
		await Task.CompletedTask;
	}
}
