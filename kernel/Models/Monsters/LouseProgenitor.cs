using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class LouseProgenitor : MonsterModel
{
	public const string curlTrigger = "Curl";

	private const string _uncurlTrigger = "Uncurl";

	private const string _webTrigger = "Web";

	private bool _curled;

	private const int _webFrail = 2;

	private const int _growStrength = 5;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 138, 134);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 141, 136);

	public bool Curled
	{
		get
		{
			return _curled;
		}
		set
		{
			AssertMutable();
			_curled = value;
		}
	}

	private int WebDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

	private int PounceDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

	private int CurlBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 14);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<CurlUpPower>(base.Creature, CurlBlock, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("WEB_CANNON_MOVE", SyncMove(WebMove), new SingleAttackIntent(WebDamage), new DebuffIntent());
		MoveState moveState2 = new MoveState("POUNCE_MOVE", SyncMove(PounceMove), new SingleAttackIntent(PounceDamage));
		MoveState moveState3 = (MoveState)(moveState.FollowUpState = new MoveState("CURL_AND_GROW_MOVE", SyncMove(CurlAndGrowMove), new DefendIntent(), new BuffIntent()));
		moveState3.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState;
		list.Add(moveState3);
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void WebMove(IReadOnlyList<Creature> targets)
	{
		if (Curled)
		{
			Curled = false;
		}
		DamageCmd.Attack(WebDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<FrailPower>(targets, 2m, base.Creature, null);
	}

	private void CurlAndGrowMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.GainBlock(base.Creature, CurlBlock, ValueProp.Move, null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 5m, base.Creature, null);
		Curled = true;
	}

	private void PounceMove(IReadOnlyList<Creature> targets)
	{
		if (Curled)
		{
			Curled = false;
		}
		DamageCmd.Attack(PounceDamage).FromMonster(this)
			.Execute(null);
	}

	
}

