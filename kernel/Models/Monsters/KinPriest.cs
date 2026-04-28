using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class KinPriest : MonsterModel
{
	private const string _theKinCustomTrackName = "the_kin_progress";

	private static readonly LocString _ritualApplyLine = MonsterModel.L10NMonsterLookup("KIN_PRIEST.moves.RITUAL.speakLine1");

	private static readonly LocString _followersDeathLine = MonsterModel.L10NMonsterLookup("KIN_PRIEST.followersDeathLine");

	private const string _grenadeTrigger = "AttackGrenade";

	private const string _laserTrigger = "AttackLaser";

	private const string _rallyTrigger = "Rally";

	private const string _attackGrenadeAnimId = "attack_grenade";

	private const int _beamRepeat = 3;

	public override string BestiaryAttackAnimId => "attack_grenade";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 199, 190);

	public override int MaxInitialHp => MinInitialHp;

	private int OrbOfFrailtyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int OrbOfWeaknessDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 3);

	private int RitualStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("ORB_OF_FRAILTY_MOVE", SyncMove(OrbOfFrailtyMove), new SingleAttackIntent(OrbOfFrailtyDamage), new DebuffIntent());
		MoveState moveState2 = new MoveState("ORB_OF_WEAKNESS_MOVE", SyncMove(OrbOfWeaknessMove), new SingleAttackIntent(OrbOfWeaknessDamage), new DebuffIntent());
		MoveState moveState3 = new MoveState("BEAM_MOVE", SyncMove(BeamMove), new MultiAttackIntent(BeamDamage, 3));
		MoveState moveState4 = new MoveState("RITUAL_MOVE", SyncMove(RitualMove), new BuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void OrbOfFrailtyMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(OrbOfFrailtyDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<FrailPower>(targets, 1m, base.Creature, null);
	}

	private void OrbOfWeaknessMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(OrbOfWeaknessDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<WeakPower>(targets, 1m, base.Creature, null);
	}

	private void BeamMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BeamDamage).WithHitCount(3).FromMonster(this)
			.Execute(null);
	}

	private void RitualMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, RitualStrength, base.Creature, null);
	}
}

