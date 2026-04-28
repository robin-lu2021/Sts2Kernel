using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class KnowledgeDemon : MonsterModel
{
	public interface IChoosable
	{
		void OnChosen();
	}

	private const string _knowledgeDemonCustomTrackName = "knowledge_demon_progress";

	private static readonly LocString _curseOfKnowledgeStartLine = MonsterModel.L10NMonsterLookup("KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.startLine");

	private static readonly LocString _curseOfKnowledgeDoneLine = MonsterModel.L10NMonsterLookup("KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.doneLine");

	private static readonly int[] _disintegrationDamageValues = new int[3] { 6, 7, 8 };

	private static readonly IReadOnlyList<IReadOnlyList<IChoosable>> _curseOfKnowledgeSets = new global::_003C_003Ez__ReadOnlyArray<IReadOnlyList<IChoosable>>(new IReadOnlyList<IChoosable>[3]
	{
		new global::_003C_003Ez__ReadOnlyArray<IChoosable>(new IChoosable[2]
		{
			(IChoosable)KernelModelDb.Card<Disintegration>(),
			(IChoosable)KernelModelDb.Card<MindRot>()
		}),
		new global::_003C_003Ez__ReadOnlyArray<IChoosable>(new IChoosable[2]
		{
			(IChoosable)KernelModelDb.Card<Disintegration>(),
			(IChoosable)KernelModelDb.Card<Sloth>()
		}),
		new global::_003C_003Ez__ReadOnlyArray<IChoosable>(new IChoosable[2]
		{
			(IChoosable)KernelModelDb.Card<Disintegration>(),
			(IChoosable)KernelModelDb.Card<WasteAway>()
		})
	});

	private int _curseOfKnowledgeCounter;

	private const int _knowledgeOverwhelmingRepeat = 3;

	private const int _ponderHeal = 30;

	private bool _isBurnt;

	private const string _mindRotTrigger = "MindRotTrigger";

	private const string _lightAttackTrigger = "LightAttackTrigger";

	private const string _mediumAttackTrigger = "MediumAttackTrigger";

	private const string _heavyAttackTrigger = "HeavyAttackTrigger";

	private const string _healTrigger = "HealTrigger";

	private int CurseOfKnowledgeCounter
	{
		get
		{
			return _curseOfKnowledgeCounter;
		}
		set
		{
			AssertMutable();
			_curseOfKnowledgeCounter = value;
		}
	}

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 399, 379);

	public override int MaxInitialHp => MinInitialHp;

	private int SlapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 17);

	private int PonderDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 11);

	private int KnowledgeOverwhelmingDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int PonderStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	public bool IsBurnt
	{
		get
		{
			return _isBurnt;
		}
		set
		{
			AssertMutable();
			_isBurnt = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("CURSE_OF_KNOWLEDGE_MOVE", SyncMove(CurseOfKnowledge), new DebuffIntent());
		MoveState moveState2 = new MoveState("SLAP_MOVE", SyncMove(SlapMove), new SingleAttackIntent(SlapDamage));
		MoveState moveState3 = new MoveState("KNOWLEDGE_OVERWHELMING_MOVE", SyncMove(KnowledgeOverwhelmingMove), new MultiAttackIntent(KnowledgeOverwhelmingDamage, 3));
		MoveState moveState4 = new MoveState("PONDER_MOVE", SyncMove(PonderMove), new SingleAttackIntent(PonderDamage), new HealIntent(), new BuffIntent());
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("CurseOfKnowledgeBranch");
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = conditionalBranchState;
		conditionalBranchState.AddState(moveState, () => _curseOfKnowledgeCounter < 3);
		conditionalBranchState.AddState(moveState2, () => _curseOfKnowledgeCounter >= 3);
		list.Add(conditionalBranchState);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState4);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void CurseOfKnowledge(IReadOnlyList<Creature> targets)
	{
		if (CurseOfKnowledgeCounter >= _curseOfKnowledgeSets.Count)
		{
			throw new InvalidOperationException($"There are no valid sets at this index {CurseOfKnowledgeCounter}");
		}
		foreach (Creature target in targets)
		{
			ChooseCurse(target);
		}
		
		CurseOfKnowledgeCounter++;
	}

	private void ChooseCurse(Creature target)
	{
		if (target.IsDead)
		{
			return;
		}
		int disintegrationDamage = _disintegrationDamageValues[CurseOfKnowledgeCounter];
		List<CardModel> cards = _curseOfKnowledgeSets[CurseOfKnowledgeCounter].Select(delegate(IChoosable c)
		{
			CardModel cardModel2 = base.CombatState.CreateCard((CardModel)c, target.Player);
			if (cardModel2 is Disintegration)
			{
				cardModel2.DynamicVars["DisintegrationPower"].BaseValue = disintegrationDamage;
			}
			return cardModel2;
		}).ToList();
		CardModel? cardModel = CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), cards, target.Player);
		if (cardModel != null)
		{
			((IChoosable)cardModel).OnChosen();
		}
	}

	private void SlapMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SlapDamage).FromMonster(this)
			.Execute(null);
	}

	private void KnowledgeOverwhelmingMove(IReadOnlyList<Creature> targets)
	{
		IsBurnt = true;
		DamageCmd.Attack(KnowledgeOverwhelmingDamage).WithHitCount(3).FromMonster(this)
			.Execute(null);
	}

	private void PonderMove(IReadOnlyList<Creature> targets)
	{
		IsBurnt = false;
		DamageCmd.Attack(PonderDamage).FromMonster(this)
			.Execute(null);
		CreatureCmd.Heal(base.Creature, 30 * base.Creature.CombatState.Players.Count);
		PowerCmd.Apply<StrengthPower>(base.Creature, PonderStrength, base.Creature, null);
	}

	
}


