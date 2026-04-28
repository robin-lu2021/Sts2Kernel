using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Monsters;
namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class ReattachPower : PowerModel
{
	private class Data
	{
		public bool isReviving;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override bool ShouldScaleInMultiplayer => true;

	private bool IsReviving => GetInternalData<Data>().isReviving;

	protected override object InitInternalData()
	{
		return new Data();
	}

	public void DoReattach()
	{
		if (!AreAllOtherSegmentsDead())
		{
			GetInternalData<Data>().isReviving = false;
			CreatureCmd.Heal(base.Owner, base.Amount);
		}
	}

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented || base.Owner != creature)
		{
			return;
		}
		if (!AreAllOtherSegmentsDead() || !base.Owner.IsDead)
		{
			GetInternalData<Data>().isReviving = true;
			if (creature.Monster is DecimillipedeSegment decimillipedeSegment)
			{
				base.Owner.Monster.SetMoveImmediate(decimillipedeSegment.DeadState);
			}
		}
		else
		{ // 死完了
			;
		}
	}

	public override bool ShouldAllowHitting(Creature creature)
	{
		if (creature != base.Owner)
		{
			return true;
		}
		if (IsReviving)
		{
			return false;
		}
		return true;
	}

	public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		if (creature != base.Owner)
		{
			return true;
		}
		return false;
	}

	public override bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return false;
	}

	public override bool ShouldOwnerDeathTriggerFatal()
	{
		return AreAllOtherSegmentsDead();
	}

	private IEnumerable<Creature> GetOtherSegments()
	{
		return from c in base.Owner.CombatState.GetTeammatesOf(base.Owner).Except(new global::_003C_003Ez__ReadOnlySingleElementList<Creature>(base.Owner))
			where c.HasPower<ReattachPower>()
			select c;
	}

	private bool AreAllOtherSegmentsDead()
	{
		return GetOtherSegments().All((Creature s) => s.IsDead);
	}
}
