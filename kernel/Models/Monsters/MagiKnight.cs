using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class MagiKnight : MonsterModel
{
	private static readonly LocString _dampenDialogue = new LocString("powers", "DAMPEN_POWER.banter");

	private const string _bombTrigger = "BombCast";

	private const string _ramAttackTrigger = "RamAttack";

	private const string _shieldTrigger = "ShieldAttack";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 89, 82);

	public override int MaxInitialHp => MinInitialHp;

	private int PowerShieldDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	private int PowerShieldBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 9, 5);

	private int SpearDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

	private int BombDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 35);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("FIRST_POWER_SHIELD_MOVE", SyncMove(PowerShieldMove), new SingleAttackIntent(PowerShieldDamage), new DefendIntent());
		MoveState moveState2 = new MoveState("DAMPEN_MOVE", SyncMove(DampenMove), new DebuffIntent());
		MoveState moveState3 = new MoveState("PREP_MOVE", SyncMove(PrepMove), new DefendIntent());
		MoveState moveState4 = new MoveState("MAGIC_BOMB", SyncMove(MagicBombMove), new SingleAttackIntent(BombDamage));
		MoveState moveState5 = new MoveState("RAM_MOVE", SyncMove(SpearMove), new SingleAttackIntent(SpearDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState5;
		moveState5.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState5;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState5);
		list.Add(moveState3);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void DampenMove(IReadOnlyList<Creature> targets)
	{
		foreach (Creature target in targets)
		{
			DampenPower dampenPower = target.GetPower<DampenPower>();
			bool flag = dampenPower == null;
			if (flag)
			{
				dampenPower = (DampenPower)KernelModelDb.Power<DampenPower>().ToMutable();
			}
			dampenPower.AddCaster(base.Creature);
			if (flag)
			{
				PowerCmd.Apply(dampenPower, target, 1m, base.Creature, null);
			}
		}
	}

	private void PowerShieldMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PowerShieldDamage).FromMonster(this)
			
			
			.Execute(null);
		CreatureCmd.GainBlock(base.Creature, PowerShieldBlock, ValueProp.Move, null);
	}

	private void PrepMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.GainBlock(base.Creature, PowerShieldBlock, ValueProp.Move, null);
	}

	private void MagicBombMove(IReadOnlyList<Creature> targets)
	{
		if (TestMode.IsOff)
		{
			Vector2? vector = null;
			foreach (Creature target in targets)
			{
				NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(target);
				if (!vector.HasValue || vector.Value.X > creatureNode.GlobalPosition.X)
				{
					vector = creatureNode.GlobalPosition;
				}
			}
			NCreature creatureNode2 = NCombatRoom.Instance.GetCreatureNode(base.Creature);
			Node2D specialNode = creatureNode2.GetSpecialNode<Node2D>("Visuals/AttackDistanceControl");
			if (specialNode != null)
			{
				float x = creatureNode2.Visuals.Body.Scale.X;
				specialNode.Position = Vector2.Left * ((creatureNode2.GlobalPosition.X - vector.Value.X - 600f) / x);
			}
		}
		DamageCmd.Attack(BombDamage).FromMonster(this)
			
			
			.Execute(null);
	}

	private void SpearMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SpearDamage).FromMonster(this)
			
			
			.Execute(null);
	}

	
}

