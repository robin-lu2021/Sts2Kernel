using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class VineShambler : MonsterModel
{
	private const string _vineShamblerVfxPath = "vfx/monsters/vine_shambler_vines/vine_shambler_vines_vfx";

	private const int _swipeRepeat = 2;

	private const string _swipeTrigger = "SwipePower";

	private const string _vinesTrigger = "Vines";

	private const string _chompTrigger = "Chomp";

	private const string _chomp = "event:/sfx/enemy/enemy_attacks/vine_shambler/vine_shambler_chomp";

	private const string _defensiveSwipe = "event:/sfx/enemy/enemy_attacks/vine_shambler/vine_shambler_defensive_swipe";

	private const string _graspingVines = "event:/sfx/enemy/enemy_attacks/vine_shambler/vine_shambler_cast";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 64, 61);

	public override int MaxInitialHp => MinInitialHp;

	private int GraspingVinesDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int SwipeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("GRASPING_VINES_MOVE", SyncMove(GraspingVinesMove), new SingleAttackIntent(GraspingVinesDamage), new CardDebuffIntent());
		MoveState moveState2 = new MoveState("SWIPE_MOVE", SyncMove(SwipeMove), new MultiAttackIntent(SwipeDamage, 2));
		MoveState moveState3 = new MoveState("CHOMP_MOVE", SyncMove(ChompMove), new SingleAttackIntent(ChompDamage));
		moveState2.FollowUpState = moveState;
		moveState.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState2);
	}

	private void GraspingVinesMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(GraspingVinesDamage).FromMonster(this)
			
			
			
			
			.Execute(null);
		PowerCmd.Apply<TangledPower>(targets, 1m, base.Creature, null);
	}

	private void SwipeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SwipeDamage).WithHitCount(2).FromMonster(this)
			.OnlyPlayAnimOnce()
			
			
			
			.Execute(null);
	}

	private void ChompMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ChompDamage).FromMonster(this)
			
			
			.Execute(null);
	}

	
}

