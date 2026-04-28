using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class NemesisPower : PowerModel
{
	private bool _shouldApplyIntangible;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != base.Owner.Side)
		{
			return;
		}
		_shouldApplyIntangible = !_shouldApplyIntangible;
		if (_shouldApplyIntangible)
		{
			IntangiblePower intangiblePower = PowerCmd.Apply<IntangiblePower>(base.Owner, 1m, base.Owner, null);
			if (intangiblePower != null)
			{
				intangiblePower.SkipNextDurationTick = true;
			}
		}
		else if (base.Owner.HasPower<IntangiblePower>())
		{
			PowerCmd.Remove(base.Owner.GetPower<IntangiblePower>());
		}
	}
}
