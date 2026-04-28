using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class TheInsatiable : MonsterModel
{
	private const int _liquifyStatusDrawCount = 3;

	private const int _liquifyStatusDiscardCount = 3;

	private const int _thrashRepeat = 2;

	private const string _liquifySandTrigger = "LiquifySand";

	private const string _salivateTrigger = "Salivate";

	private const string _biteTrigger = "Bite";

	private const string _thrashTrigger = "Thrash";

	public const string eatPlayerTrigger = "EatPlayerTrigger";

	private bool _hasLiquified;

	public static string TheInsatiableTrackName => "insatiable_progress";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 341, 321);

	public override int MaxInitialHp => MinInitialHp;

	private int ThrashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 31, 28);

	private int SalivateStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	private bool HasLiquified
	{
		get
		{
			return _hasLiquified;
		}
		set
		{
			AssertMutable();
			_hasLiquified = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("LIQUIFY_GROUND_MOVE", SyncMove(LiquifyMove), new BuffIntent(), new StatusIntent(6));
		MoveState moveState2 = new MoveState("THRASH_MOVE_1", SyncMove(ThrashMove), new MultiAttackIntent(ThrashDamage, 2));
		MoveState moveState3 = new MoveState("THRASH_MOVE_2", SyncMove(ThrashMove), new MultiAttackIntent(ThrashDamage, 2));
		MoveState moveState4 = new MoveState("LUNGING_BITE_MOVE", SyncMove(BiteMove), new SingleAttackIntent(BiteDamage));
		MoveState moveState5 = new MoveState("SALIVATE_MOVE", SyncMove(SalivateMove), new BuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState5;
		moveState5.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState4);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState5);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void LiquifyMove(IReadOnlyList<Creature> targets)
	{
		foreach (Creature target in targets)
		{
			SandpitPower sandpitPower = (SandpitPower)KernelModelDb.Power<SandpitPower>().ToMutable();
			sandpitPower.Target = target;
			PowerCmd.Apply(sandpitPower, base.Creature, 4m, base.Creature, null);
		}
		foreach (Creature target2 in targets)
		{
			Player player = target2.Player ?? target2.PetOwner;
			List<CardPileAddResult> statusCards = new List<CardPileAddResult>();
			for (int i = 0; i < 6; i++)
			{
				CardModel card = base.CombatState.CreateCard<FranticEscape>(player);
				PileType newPileType = ((i < 3) ? PileType.Draw : PileType.Discard);
				List<CardPileAddResult> list = statusCards;
				list.Add(CardPileCmd.AddGeneratedCardToCombat(card, newPileType, addedByPlayer: false, CardPilePosition.Random));
			}
			if (LocalContext.IsMe(player))
			{
				CardCmd.PreviewCardPileAdd(statusCards);
			}
		}
		HasLiquified = true;
	}

	private void ThrashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ThrashDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
	}

	private void BiteMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BiteDamage).FromMonster(this)
			.Execute(null);
	}

	private void SalivateMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, SalivateStrength, base.Creature, null);
	}

	
}

