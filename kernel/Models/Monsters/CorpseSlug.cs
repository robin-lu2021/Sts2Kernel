using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class CorpseSlug : MonsterModel
{
	private const string _heavyAttackTrigger = "HeavyAttackTrigger";

	public const string devourStartTrigger = "DevourStartTrigger";

	public const string devourEndTrigger = "DevourEndkTrigger";

	private bool _isRavenous;

	private int _starterMoveIdx;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 27, 25);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 29, 27);

	private int WhipSlapDamage => 3;

	private int WhipSlapRepeat => 2;

	private int GlompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int GoopFrailAmt => 2;

	private int RavenousStr => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

	public bool IsRavenous
	{
		get
		{
			return _isRavenous;
		}
		set
		{
			AssertMutable();
			_isRavenous = value;
		}
	}

	public int StarterMoveIdx
	{
		get
		{
			return _starterMoveIdx;
		}
		set
		{
			AssertMutable();
			_starterMoveIdx = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<RavenousPower>(base.Creature, RavenousStr, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("WHIP_SLAP_MOVE", SyncMove(WhipSlapMove), new MultiAttackIntent(WhipSlapDamage, WhipSlapRepeat));
		MoveState moveState2 = new MoveState("GLOMP_MOVE", SyncMove(GlompMove), new SingleAttackIntent(GlompDamage));
		MoveState moveState3 = new MoveState("GOOP_MOVE", SyncMove(GoopMove), new DebuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, (StarterMoveIdx % 3) switch
		{
			0 => moveState, 
			1 => moveState2, 
			_ => moveState3, 
		});
	}

	private void WhipSlapMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(WhipSlapDamage).WithHitCount(WhipSlapRepeat).FromMonster(this)
			.Execute(null);
	}

	private void GlompMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(GlompDamage).FromMonster(this)
			.Execute(null);
	}

	private void GoopMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<FrailPower>(targets, GoopFrailAmt, base.Creature, null);
	}

	public static void EnsureCorpseSlugsStartWithDifferentMoves(IEnumerable<MonsterModel> monsters, Rng rng)
	{
		IEnumerable<CorpseSlug> enumerable = monsters.OfType<CorpseSlug>();
		int num = rng.NextInt(3);
		foreach (CorpseSlug item in enumerable)
		{
			item.StarterMoveIdx = num % 3;
			num++;
		}
	}

	
}

