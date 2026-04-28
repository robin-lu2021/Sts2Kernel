using MegaCrit.Sts2.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class MultiCast : CardModel
{
	protected override bool HasEnergyCostX => true;

	public override OrbEvokeType OrbEvokeType => OrbEvokeType.All;

	public MultiCast()
		: base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int evokeCount = ResolveEnergyXValue();
		if (base.IsUpgraded)
		{
			evokeCount++;
		}
		for (int i = 0; i < evokeCount; i++)
		{
			OrbCmd.EvokeNext(choiceContext, base.Owner, i == evokeCount - 1);
		}
	}
}
