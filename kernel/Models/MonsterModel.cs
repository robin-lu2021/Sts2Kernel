using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core;

public sealed class mySerializableMonster
{
	public string Id { get; set; } = string.Empty;

	public bool SpawnedThisTurn { get; set; }

	public bool IsPerformingMove { get; set; }

	public string NextMoveId { get; set; } = string.Empty;

	public Dictionary<string, string> State { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

public abstract class MonsterModel : AbstractModel
{
	private static readonly string _fallbackVisualsPath = SceneHelper.GetScenePath("creature_visuals/fallback");

	private Rng? _rng;

	private RunRngSet? _runRng;

	private Creature? _creature;

	private MonsterMoveStateMachine? _moveStateMachine;

	public virtual string ContentId => Id.Entry;

	public override bool ShouldReceiveCombatHooks => true;

	public virtual LocString Title => L10NMonsterLookup(ContentId + ".name");

	protected virtual string VisualsPath => string.Empty;

	public abstract int MinInitialHp { get; }

	public abstract int MaxInitialHp { get; }

	public virtual float HpBarSizeReduction => 0f;

	public Rng Rng
	{
		get
		{
			return _rng ?? Random.Rng.Chaotic;
		}
		set
		{
			_rng = value;
		}
	}

	public RunRngSet RunRng
	{
		get
		{
			return _runRng ?? throw new InvalidOperationException($"Monster '{ContentId}' does not have a RunRng yet.");
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			if (_runRng != null && !ReferenceEquals(_runRng, value))
			{
				throw new InvalidOperationException($"Monster '{ContentId}' cannot be moved to another RunRng.");
			}
			_runRng = value;
		}
	}

	public bool HasRunRng => _runRng != null;

	public Creature Creature
	{
		get
		{
			return _creature ?? throw new InvalidOperationException($"Monster '{ContentId}' does not have a creature yet.");
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			if (_creature != null && !ReferenceEquals(_creature, value))
			{
				throw new InvalidOperationException($"Monster '{Id}' already has a creature.");
			}
			_creature = value;
		}
	}

	public bool HasCreature => _creature != null;

	public CombatState? CombatState => _creature?.CombatState;

	public MonsterMoveStateMachine? MoveStateMachine => _moveStateMachine;

	public MoveState NextMove { get; private set; } = new MoveState();

	public bool SpawnedThisTurn { get; private set; }

	public bool IsPerformingMove { get; private set; }

	protected abstract MonsterMoveStateMachine GenerateMoveStateMachine();

	public virtual bool ShouldFadeAfterDeath => true;

	public virtual bool HasDeathSfx => true;

	public virtual bool ShouldDisappearFromDoom => true;

	public virtual bool CanChangeScale => true;

	public virtual float DeathAnimLengthOverride => 0f;

	public virtual string BestiaryAttackAnimId => string.Empty;

	public virtual string HurtSfx => string.Empty;

	public virtual string AttackSfx => string.Empty;

	public virtual string CastSfx => string.Empty;

	public virtual IEnumerable<string> AssetPaths => Array.Empty<string>();

	public bool IntendsToAttack => NextMove.Intents.Any(delegate(AbstractIntent intent)
	{
		IntentType intentType = intent.IntentType;
		return (intentType == IntentType.Attack || intentType == IntentType.DeathBlow) ? true : false;
	});

	public virtual void AfterAddedToRoom()
	{
		return;
	}

	public virtual void BeforeRemovedFromRoom()
	{
	}

	public virtual void OnDieToDoom()
	{
	}

	public virtual MonsterModel ToMutable()
	{
		return (MonsterModel)MutableClone();
	}

	protected static Func<IReadOnlyList<Creature>, Task> SyncMove(Action<IReadOnlyList<Creature>> move)
	{
		return (IReadOnlyList<Creature> targets) =>
		{
			move(targets);
			return Task.CompletedTask;
		};
	}

	public void ResetStateMachine()
	{
		_moveStateMachine = null;
		NextMove = new MoveState();
	}

	public void SetUpForCombat()
	{
		_moveStateMachine = GenerateMoveStateMachine();
		SpawnedThisTurn = true;
	}

	public void RollMove(IEnumerable<Creature> targets)
	{
		if (_moveStateMachine == null)
		{
			throw new InvalidOperationException($"Monster '{Id}' has not been set up for combat.");
		}
		if (!HasCreature)
		{
			throw new InvalidOperationException($"Monster '{Id}' cannot roll a move before Creature is assigned.");
		}
		if (!HasRunRng)
		{
			throw new InvalidOperationException($"Monster '{Id}' cannot roll a move before RunRng is assigned.");
		}
		NextMove = _moveStateMachine.RollMove(targets, Creature, RunRng.MonsterAi);
	}

	public void SetMoveImmediate(MoveState state, bool forceTransition = false)
	{
		if (state == null)
		{
			throw new ArgumentNullException(nameof(state));
		}
		if (_moveStateMachine == null)
		{
			throw new InvalidOperationException($"Monster '{Id}' has not been set up for combat.");
		}
		if (NextMove.CanTransitionAway || forceTransition)
		{
			NextMove = state;
			_moveStateMachine.ForceCurrentState(state);
		}
	}

	public void PerformMove()
	{
		if (!HasCreature)
		{
			throw new InvalidOperationException($"Monster '{Id}' cannot perform a move before Creature is assigned.");
		}
		if (_moveStateMachine == null)
		{
			throw new InvalidOperationException($"Monster '{Id}' has not been set up for combat.");
		}
		IsPerformingMove = true;
		try
		{
			MoveState move = NextMove;
			IReadOnlyList<Creature> targets = Creature.CombatState?.PlayerCreatures ?? Array.Empty<Creature>();
			move.PerformMove(targets);
			_moveStateMachine.OnMovePerformed(move);
		}
		finally
		{
			IsPerformingMove = false;
		}
	}

	public void OnSideSwitch()
	{
		SpawnedThisTurn = false;
	}

	public virtual List<BestiaryMonsterMove> MonsterMoveList()
	{
		return new List<BestiaryMonsterMove>();
	}

	public mySerializableMonster SaveState()
	{
		mySerializableMonster save = new mySerializableMonster
		{
			Id = ContentId,
			SpawnedThisTurn = SpawnedThisTurn,
			IsPerformingMove = IsPerformingMove,
			NextMoveId = NextMove.Id
		};
		WriteCustomState(save.State);
		return save;
	}

	public virtual void LoadState(mySerializableMonster save)
	{
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		if (!string.IsNullOrWhiteSpace(save.Id) && !string.Equals(save.Id, ContentId, StringComparison.Ordinal))
		{
			throw new InvalidOperationException($"Cannot load monster state for '{save.Id}' into '{ContentId}'.");
		}
		SpawnedThisTurn = save.SpawnedThisTurn;
		IsPerformingMove = save.IsPerformingMove;
		ReadCustomState(save.State);
	}

	public static TMonster RestoreState<TMonster>(mySerializableMonster save)
		where TMonster : MonsterModel, new()
	{
		TMonster monster = new TMonster();
		monster.LoadState(save);
		return monster;
	}

	protected virtual void WriteCustomState(Dictionary<string, string> state)
	{
	}

	protected virtual void ReadCustomState(IReadOnlyDictionary<string, string> state)
	{
	}

	public static LocString L10NMonsterLookup(string entryName)
	{
		return new LocString("monsters", entryName);
	}
}
