using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class GlobeHead : MonsterModel
{
	private const int _thunderStrikeRepeat = 3;

	private const int _shockingSlapFrail = 2;

	private const int _galvanicBurstStr = 2;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 158, 148);

	public override int MaxInitialHp => MinInitialHp;

	private int ThunderStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	private int ShockingSlapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);

	private int GalvanicBurstDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 16);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<GalvanicPower>(base.Creature, 6m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("THUNDER_STRIKE", SyncMove(ThunderStrike), new MultiAttackIntent(ThunderStrikeDamage, 3));
		MoveState moveState2 = new MoveState("SHOCKING_SLAP", SyncMove(ShockingSlap), new SingleAttackIntent(ShockingSlapDamage), new DebuffIntent());
		MoveState moveState3 = new MoveState("GALVANIC_BURST", SyncMove(GalvanicBurstMove), new SingleAttackIntent(GalvanicBurstDamage), new BuffIntent());
		moveState2.FollowUpState = moveState;
		moveState.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState2;
		list.Add(moveState2);
		list.Add(moveState);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState2);
	}

	private void ThunderStrike(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ThunderStrikeDamage).WithHitCount(3).FromMonster(this)
			
			.OnlyPlayAnimOnce()
			
			
			.Execute(null);
	}

	private void ShockingSlap(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ShockingSlapDamage).FromMonster(this)
			
			
			.Execute(null);
		PowerCmd.Apply<FrailPower>(targets, 2m, base.Creature, null);
	}

	private void GalvanicBurstMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(GalvanicBurstDamage).FromMonster(this)
			
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}
}

