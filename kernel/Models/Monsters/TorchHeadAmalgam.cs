using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class TorchHeadAmalgam : MonsterModel
{
	private const int _soulBeamRepeat = 3;

	private const string _debuffTrigger = "DebuffTrigger";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 211, 199);

	public override int MaxInitialHp => MinInitialHp;

	private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 18);

	private int WeakTackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 14);

	private int SoulBeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 8);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("TACKLE_1_MOVE", SyncMove(TackleMove), new SingleAttackIntent(TackleDamage));
		MoveState moveState2 = new MoveState("TACKLE_2_MOVE", SyncMove(TackleMove), new SingleAttackIntent(TackleDamage));
		MoveState moveState3 = new MoveState("BEAM_MOVE", SyncMove(SoulBeamMove), new MultiAttackIntent(SoulBeamDamage, 3));
		MoveState moveState4 = new MoveState("TACKLE_3_MOVE", SyncMove(WeakTackleMove), new SingleAttackIntent(WeakTackleDamage));
		MoveState moveState5 = new MoveState("TACKLE_4_MOVE", SyncMove(WeakTackleMove), new SingleAttackIntent(WeakTackleDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState5;
		moveState5.FollowUpState = moveState3;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		list.Add(moveState5);
		return new MonsterMoveStateMachine(list, moveState);
	}

	public override void OnDieToDoom()
	{
		if (TestMode.IsOff)
		{
			NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(base.Creature);
			if (creatureNode != null)
			{
				creatureNode.GetSpecialNode<Node2D>("Visuals/torch1Slot/fire1_small_green/light_small")?.SetVisible(visible: false);
				creatureNode.GetSpecialNode<Node2D>("Visuals/torch2Slot/fire2_small_green/light_small")?.SetVisible(visible: false);
				creatureNode.GetSpecialNode<Node2D>("Visuals/torch3Slot/fire3_small_green/light_small")?.SetVisible(visible: false);
			}
		}
	}

	private void TackleMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(TackleDamage).FromMonster(this)
			.Execute(null);
	}

	private void WeakTackleMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(WeakTackleDamage).FromMonster(this)
			.Execute(null);
	}

	private void SoulBeamMove(IReadOnlyList<Creature> targets)
	{
		NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
		if (nCreature != null)
		{
			Node2D specialNode = nCreature.GetSpecialNode<Node2D>("Visuals/LaserControlBone");
			if (specialNode != null)
			{
				NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(targets[0]);
				specialNode.Position += Vector2.Left * (creatureNode.GlobalPosition.X - nCreature.GlobalPosition.X + 3000f);
			}
		}
		DamageCmd.Attack(SoulBeamDamage).WithHitCount(3).FromMonster(this)
			.Execute(null);
	}

	
}

