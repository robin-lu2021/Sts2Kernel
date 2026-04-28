using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class SurroundedPower : PowerModel
{
	public enum Direction
	{
		Right,
		Left
	}

	private Direction _facing;

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Single;

	public Direction Facing
	{
		get
		{
			return _facing;
		}
		private set
		{
			AssertMutable();
			_facing = value;
		}
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (dealer == null)
		{
			return 1m;
		}
		if (target != base.Owner)
		{
			return 1m;
		}
		switch (Facing)
		{
		case Direction.Right:
			if (!dealer.HasPower<BackAttackLeftPower>())
			{
				return 1m;
			}
			break;
		case Direction.Left:
			if (!dealer.HasPower<BackAttackRightPower>())
			{
				return 1m;
			}
			break;
		}
		return 1.5m;
	}

	public override void BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Target != null && cardPlay.Card.Owner == base.Owner.Player)
		{
			UpdateDirection(cardPlay.Target);
		}
	}

	public override void BeforePotionUsed(PotionModel potion, Creature? target)
	{
		if (CombatManager.Instance.IsInProgress && target != null && potion.Owner == base.Owner.Player)
		{
			UpdateDirection(target);
		}
	}

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (!wasRemovalPrevented && creature.Side != base.Owner.Side)
		{
			IReadOnlyList<Creature> hittableEnemies = base.Owner.CombatState.HittableEnemies;
			if (hittableEnemies.Count != 0 && (hittableEnemies.All((Creature e) => e.HasPower<BackAttackLeftPower>()) || hittableEnemies.All((Creature e) => e.HasPower<BackAttackRightPower>())))
			{
				UpdateDirection(hittableEnemies[0]);
			}
		}
	}

	private void UpdateDirection(Creature target)
	{
		switch (Facing)
		{
		case Direction.Right:
			if (target.HasPower<BackAttackLeftPower>())
			{
				FaceDirection(Direction.Left);
			}
			break;
		case Direction.Left:
			if (target.HasPower<BackAttackRightPower>())
			{
				FaceDirection(Direction.Right);
			}
			break;
		}
	}

	private void FaceDirection(Direction direction)
	{
		Facing = direction;
	}
}
