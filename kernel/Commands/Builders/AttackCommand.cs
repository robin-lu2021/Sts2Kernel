using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Commands.Builders;

public class AttackCommand
{
	private enum SourceType
	{
		None,
		Card,
		Monster
	}

	private readonly decimal _damagePerHit;

	private readonly CalculatedDamageVar? _calculatedDamageVar;

	private int _hitCount = 1;

	private SourceType _sourceType;

	private CombatState? _combatState;

	private Creature? _singleTarget;

	private bool _doesRandomTargetingAllowDuplicates = true;

	private readonly List<DamageResult> _results = new();

	private string? _attackerAnimName;

	private float _attackerAnimDelay;

	private Creature? _visualAttacker;

	private bool _playOnEveryHit = true;

	private bool _shouldPlayAnimation = true;

	private readonly float[] _waitBeforeHit = new float[2] { -1f, -1f };

	private Action? _afterAttackerAnim;

	private Action? _beforeDamage;

	public Creature? Attacker { get; private set; }

	public AbstractModel? ModelSource { get; private set; }

	public CombatSide TargetSide { get; private set; }

	public ValueProp DamageProps { get; private set; } = ValueProp.Move;

	public bool IsSingleTargeted => _singleTarget != null;

	public bool IsMultiTargeted => _combatState != null;

	public bool IsRandomlyTargeted { get; private set; }

	public IEnumerable<DamageResult> Results => _results;

	private IReadOnlyList<Creature> GetPossibleTargets()
	{
		if (IsSingleTargeted)
		{
			return new global::_003C_003Ez__ReadOnlySingleElementList<Creature>(_singleTarget!);
		}
		if (IsMultiTargeted)
		{
			if (_sourceType == SourceType.Monster)
			{
				return _combatState!.PlayerCreatures;
			}
			if (Attacker == null)
			{
				throw new InvalidOperationException("We require an attacker to be able to grab its opponents");
			}
			return _combatState!.GetOpponentsOf(Attacker);
		}
		throw new InvalidOperationException("No targets set, a Targeting method must be called before Execute");
	}

	public AttackCommand(decimal damagePerHit)
	{
		_damagePerHit = damagePerHit;
	}

	public AttackCommand(CalculatedDamageVar calculatedDamageVar)
	{
		_damagePerHit = -1m;
		_calculatedDamageVar = calculatedDamageVar;
	}

	public AttackCommand FromCard(CardModel card)
	{
		if (Attacker != null)
		{
			throw new InvalidOperationException("Attacker has already been set.");
		}
		if (ModelSource != null)
		{
			throw new InvalidOperationException("ModelSource has already been set.");
		}
		Player owner = card.Owner;
		Attacker = owner.Creature;
		_attackerAnimName = "Attack";
		_attackerAnimDelay = owner.Character.AttackAnimDelay;
		ModelSource = card;
		_sourceType = SourceType.Card;
		return this;
	}

	public AttackCommand FromOsty(Creature osty, CardModel card)
	{
		Attacker = osty;
		ModelSource = card;
		_attackerAnimName = "Attack";
		_attackerAnimDelay = 0.3f;
		_sourceType = SourceType.Card;
		return this;
	}

	public AttackCommand FromMonster(MonsterModel monster)
	{
		if (Attacker != null)
		{
			throw new InvalidOperationException("Attacker has already been set.");
		}
		Attacker = monster.Creature;
		_attackerAnimName = "Attack";
		_sourceType = SourceType.Monster;
		return TargetingAllOpponents(monster.Creature.CombatState);
	}

	public AttackCommand Targeting(Creature target)
	{
		if (_singleTarget != null || _combatState != null)
		{
			throw new InvalidOperationException("Targets already set.");
		}
		_singleTarget = target;
		TargetSide = target.Side;
		return this;
	}

	public AttackCommand TargetingAllOpponents(CombatState combatState)
	{
		if (_singleTarget != null || _combatState != null)
		{
			throw new InvalidOperationException("Targets already set.");
		}
		if (Attacker == null)
		{
			throw new InvalidOperationException("We require an attacker to be able to grab its opponents");
		}
		_combatState = combatState;
		TargetSide = Attacker.Side == CombatSide.Enemy ? CombatSide.Player : CombatSide.Enemy;
		return this;
	}

	public AttackCommand TargetingRandomOpponents(CombatState combatState, bool allowDuplicates = true)
	{
		TargetingAllOpponents(combatState);
		IsRandomlyTargeted = true;
		_doesRandomTargetingAllowDuplicates = allowDuplicates;
		return this;
	}

	public AttackCommand Unpowered()
	{
		DamageProps |= ValueProp.Unpowered;
		return this;
	}

	public AttackCommand WithAttackerAnim(string? animName, float delay, Creature? visualAttacker = null)
	{
		_attackerAnimName = animName;
		_attackerAnimDelay = delay;
		_visualAttacker = visualAttacker;
		return this;
	}

	public AttackCommand WithNoAttackerAnim()
	{
		_shouldPlayAnimation = false;
		return this;
	}

	public AttackCommand AfterAttackerAnim(Action afterAttackerAnim)
	{
		_afterAttackerAnim = afterAttackerAnim;
		return this;
	}

	public AttackCommand WithWaitBeforeHit(float fastSeconds, float standardSeconds)
	{
		_waitBeforeHit[0] = fastSeconds;
		_waitBeforeHit[1] = standardSeconds;
		return this;
	}

	public AttackCommand SpawningHitVfxOnEachCreature()
	{
		return this;
	}

	public AttackCommand WithHitVfxSpawnedAtBase()
	{
		return this;
	}

	public AttackCommand WithHitVfxNode(Func<Creature, object?> createHitVfxNode)
	{
		return this;
	}

	public AttackCommand OnlyPlayAnimOnce()
	{
		_playOnEveryHit = false;
		return this;
	}

	public AttackCommand WithHitCount(int hitCount)
	{
		_hitCount = hitCount;
		return this;
	}

	public AttackCommand BeforeDamage(Action beforeDamage)
	{
		_beforeDamage = beforeDamage;
		return this;
	}

	public static AttackContext CreateContextAsync(CombatState combatState, CardModel cardSource)
	{
		return AttackContext.CreateAsync(combatState, cardSource);
	}

	public AttackCommand Execute(PlayerChoiceContext? choiceContext)
	{
		if (Attacker == null)
		{
			throw new InvalidOperationException("No attacker set.");
		}
		if (CombatManager.Instance.IsOverOrEnding || Attacker.IsDead)
		{
			return this;
		}
		if (!IsSingleTargeted && !IsMultiTargeted)
		{
			throw new InvalidOperationException("No targets set.");
		}
		CombatState combatState = Attacker.CombatState;
		Hook.BeforeAttack(combatState, this);
		decimal attackCount = Hook.ModifyAttackHitCount(combatState, this, _hitCount);
		for (int i = 0; (decimal)i < attackCount; i++)
		{
			if (Attacker.IsDead)
			{
				break;
			}
			List<Creature> validTargets = GetPossibleTargets().Where(c => c.IsAlive).ToList();
			if (validTargets.Count == 0)
			{
				break;
			}
			if (_shouldPlayAnimation && (_playOnEveryHit || i == 0))
			{
				_afterAttackerAnim?.Invoke();
			}

			Creature? singleTarget;
			if (!IsRandomlyTargeted)
			{
				singleTarget = validTargets.Count == 1 ? validTargets[0] : _singleTarget;
			}
			else
			{
				if (!_doesRandomTargetingAllowDuplicates)
				{
					validTargets = validTargets.Where(c => _results.All(r => r.Receiver != c)).ToList();
					if (validTargets.Count == 0)
					{
						throw new InvalidOperationException("No valid targets for attack with duplicates disallowed.");
					}
				}
				Rng combatTargets = (Attacker.Player ?? Attacker.PetOwner!).RunState.Rng.CombatTargets;
				singleTarget = combatTargets.NextItem(validTargets);
			}

			if (_waitBeforeHit[0] >= 0f || _waitBeforeHit[1] >= 0f)
			{
				// Headless kernel intentionally ignores timing-only waits.
			}

			_beforeDamage?.Invoke();

			IEnumerable<Creature> targets = singleTarget != null
				? new List<Creature>(1) { singleTarget }
				: validTargets;

			AddResultsInternal(CreatureCmd.Damage(
				choiceContext ?? new BlockingPlayerChoiceContext(),
				targets,
				_calculatedDamageVar == null ? _damagePerHit : _calculatedDamageVar.Calculate(singleTarget),
				DamageProps,
				Attacker,
				ModelSource as CardModel));
		}
		CombatManager.Instance.History.CreatureAttacked(combatState, Attacker, _results.ToList());
		Hook.AfterAttack(combatState, this);
		return this;
	}

	public void IncrementHitsInternal()
	{
		_hitCount++;
	}

	public void AddResultsInternal(IEnumerable<DamageResult> results)
	{
		_results.AddRange(results);
	}
}
