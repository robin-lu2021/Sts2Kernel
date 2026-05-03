using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core;

public abstract class CardModel : AbstractModel
{
	private const string SavedKeywordsKey = "__card_keywords";

	private Player? _owner;

	private CardEnergyCost? _energyCost;

	private int _baseReplayCount;

	private bool _starCostSet;

	private int _baseStarCost;

	private bool _wasStarCostJustUpgraded;

	private List<TemporaryCardCost> _temporaryStarCosts = new List<TemporaryCardCost>();

	private int _lastStarsSpent;

	private HashSet<CardKeyword>? _keywords;

	private HashSet<CardTag>? _tags;

	private DynamicVarSet? _dynamicVars;

	private bool _exhaustOnNextPlay;

	private bool _hasSingleTurnRetain;

	private bool _hasSingleTurnSly;

	private CardModel? _cloneOf;

	private bool _isDupe;

	private int _currentUpgradeLevel;

	private CardUpgradePreviewType _upgradePreviewType;

	private bool _isEnchantmentPreview;

	private int? _floorAddedToDeck;

	private Creature? _currentTarget;

	private EnchantmentModel? _enchantment;

	private AfflictionModel? _affliction;

	private CardModel? _deckVersion;

	private CardModel? _canonicalInstance;

	public virtual LocString TitleLocString => new LocString("cards", ContentId + ".title");

	public virtual string Title
	{
		get
		{
			string title = TitleLocString.GetFormattedText();
			if (!IsUpgraded)
			{
				return title;
			}
			if (MaxUpgradeLevel > 1)
			{
				return $"{title}+{CurrentUpgradeLevel}";
			}
			return title + "+";
		}
	}

	private CardPoolModel? _pool;

	public virtual string ContentId => Id.Entry;

	public virtual CardType Type { get; }

	public virtual CardRarity Rarity { get; }

	public virtual TargetType TargetType { get; }

	public virtual CardPoolModel Pool
	{
		get
		{
			if (_pool != null)
			{
				return _pool;
			}
			foreach (CardPoolModel pool in ModelDb.AllCardPools)
			{
				if (pool.AllCardIds.Contains(Id))
				{
					_pool = pool;
					return _pool;
				}
			}
			MockCardPool mockPool = ModelDb.CardPool<MockCardPool>();
			if (mockPool.AllCardIds.Contains(Id))
			{
				_pool = mockPool;
				return _pool;
			}
			throw new InvalidProgramException($"Card {this} is not in any card pool!");
		}
	}

	public virtual CardPoolModel VisualCardPool => Pool;

	protected LocString SelectionScreenPrompt
	{
		get
		{
			LocString locString = new LocString("cards", base.Id.Entry + ".selectionScreenPrompt");
			if (!locString.Exists())
			{
				throw new InvalidOperationException($"No selection screen prompt for {base.Id}.");
			}
			DynamicVars.AddTo(locString);
			return locString;
		}
	}

	protected virtual bool HasEnergyCostX => false;

	protected virtual int CanonicalEnergyCost { get; }

	public virtual bool HasStarCostX => false;

	protected virtual bool IsPlayable => true;

	public virtual CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.None;

	public Player Owner
	{
		get
		{
			AssertMutable();
			return _owner ?? throw new InvalidOperationException($"Card '{ContentId}' does not have an owner yet.");
		}
		set
		{
			AssertMutable();
			if (_owner != null && value != null)
			{
				throw new InvalidOperationException("Card " + Id.Entry + " already has an owner.");
			}
			_owner = value;
		}
	}

	public bool HasOwner => _owner != null;

	public int FloorAddedToDeck
	{
		get => _floorAddedToDeck ?? 0;
		set
		{
			AssertMutable();
			_floorAddedToDeck = value;
		}
	}

	public int CurrentUpgradeLevel
	{
		get => _currentUpgradeLevel;
		private set
		{
			AssertMutable();
			if (value > MaxUpgradeLevel)
			{
				throw new InvalidOperationException($"{base.Id} cannot be upgraded past its MaxUpgradeLevel.");
			}
			_currentUpgradeLevel = value;
		}
	}

	public virtual int MaxUpgradeLevel => 1;

	public bool IsUpgraded => CurrentUpgradeLevel > 0;

	public bool IsUpgradable => CurrentUpgradeLevel < MaxUpgradeLevel;

	public int EnergyCostForTurn { get; private set; }

	public int StarCostForTurn { get; private set; }

	public bool IsFreeToPlay { get; private set; }

	public bool ExhaustOnPlay
	{
		get => Keywords.Contains(CardKeyword.Exhaust);
		set
		{
			if (value)
			{
				AddKeyword(CardKeyword.Exhaust);
			}
			else
			{
				RemoveKeyword(CardKeyword.Exhaust);
			}
		}
	}

	public bool Retain
	{
		get => Keywords.Contains(CardKeyword.Retain);
		set
		{
			if (value)
			{
				AddKeyword(CardKeyword.Retain);
			}
			else
			{
				RemoveKeyword(CardKeyword.Retain);
			}
		}
	}

	public bool Ethereal
	{
		get => Keywords.Contains(CardKeyword.Ethereal);
		set
		{
			if (value)
			{
				AddKeyword(CardKeyword.Ethereal);
			}
			else
			{
				RemoveKeyword(CardKeyword.Ethereal);
			}
		}
	}

	public bool HasBeenRemovedFromState { get; internal set; }

	public PileType CurrentPileType { get; private set; } = PileType.None;

	public Creature? CurrentTarget
	{
		get => _currentTarget;
		private set
		{
			AssertMutable();
			_currentTarget = value;
		}
	}

	public CardEnergyCost EnergyCost => _energyCost ??= new CardEnergyCost(this, CanonicalEnergyCost, HasEnergyCostX);

	public int BaseReplayCount
	{
		get => _baseReplayCount;
		set
		{
			AssertMutable();
			_baseReplayCount = value;
			ReplayCountChanged?.Invoke();
		}
	}

	public bool ExhaustOnNextPlay
	{
		get => _exhaustOnNextPlay;
		set
		{
			AssertMutable();
			_exhaustOnNextPlay = value;
		}
	}

	public bool ShouldRetainThisTurn => Keywords.Contains(CardKeyword.Retain) || _hasSingleTurnRetain;

	public bool IsInCombat => IsMutable && (Pile?.IsCombatPile ?? false);

	public bool IsDupe
	{
		get => _isDupe;
		private set
		{
			AssertMutable();
			_isDupe = value;
		}
	}

	public CardModel? DeckVersion
	{
		get => _deckVersion;
		set
		{
			AssertMutable();
			_deckVersion = value;
		}
	}

	public CardModel CanonicalInstance => !IsMutable ? this : _canonicalInstance ?? this;

	public bool IsClone => CloneOf != null;

	public CardModel? DupeOf => IsDupe ? CloneOf : null;

	public bool IsTransformable
	{
		get
		{
			if (!IsRemovable)
			{
				CardPile? pile = Pile;
				return pile == null || pile.Type != PileType.Deck;
			}
			return true;
		}
	}

	public virtual bool IsRemovable => !Keywords.Contains(CardKeyword.Eternal);

	public bool IsSlyThisTurn => Keywords.Contains(CardKeyword.Sly) || _hasSingleTurnSly;

	public EnchantmentModel? Enchantment => _enchantment;

	public AfflictionModel? Affliction => _affliction;

	public bool IsEnchantmentPreview
	{
		get => _isEnchantmentPreview;
		set
		{
			AssertMutable();
			_isEnchantmentPreview = value;
		}
	}

	protected virtual IEnumerable<IHoverTip> ExtraHoverTips => Array.Empty<IHoverTip>();

	public IEnumerable<IHoverTip> HoverTips
	{
		get
		{
			List<IHoverTip> hoverTips = ExtraHoverTips.ToList();
			if (Enchantment != null)
			{
				hoverTips.AddRange(Enchantment.HoverTips);
			}
			if (Affliction != null)
			{
				hoverTips.AddRange(Affliction.HoverTips);
			}
			int enchantedReplayCount = GetEnchantedReplayCount();
			if (enchantedReplayCount > 0)
			{
				hoverTips.Add(HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, new DynamicVar("Times", enchantedReplayCount)));
			}
			if (OrbEvokeType != OrbEvokeType.None)
			{
				hoverTips.Add(HoverTipFactory.Static(StaticHoverTip.Evoke));
			}
			if (GainsBlock)
			{
				hoverTips.Add(HoverTipFactory.Static(StaticHoverTip.Block));
			}
			foreach (CardKeyword keyword in Keywords)
			{
				hoverTips.Add(HoverTipFactory.FromKeyword(keyword));
				if (keyword == CardKeyword.Ethereal)
				{
					hoverTips.Add(HoverTipFactory.FromKeyword(CardKeyword.Exhaust));
				}
			}
			return hoverTips.Distinct();
		}
	}

	public CardUpgradePreviewType UpgradePreviewType
	{
		get => _upgradePreviewType;
		set
		{
			AssertMutable();
			if (!value.IsPreview() && _upgradePreviewType.IsPreview())
			{
				throw new InvalidOperationException("A card cannot go to from being upgrade preview. Consider making a new card model instead.");
			}
			_upgradePreviewType = value;
		}
	}

	public bool WasStarCostJustUpgraded => _wasStarCostJustUpgraded;

	public TemporaryCardCost? TemporaryStarCost => _temporaryStarCosts.LastOrDefault();

	public virtual int CurrentStarCost
	{
		get
		{
			int? temporaryCost = _temporaryStarCosts.LastOrDefault()?.Cost;
			if (temporaryCost.HasValue)
			{
				if (temporaryCost == 0 && BaseStarCost < 0)
				{
					return BaseStarCost;
				}
				return temporaryCost.Value;
			}
			return BaseStarCost;
		}
	}

	public int BaseStarCost
	{
		get
		{
			if (!IsMutable)
			{
				return CanonicalStarCost;
			}
			if (!_starCostSet)
			{
				_baseStarCost = CanonicalStarCost;
				_starCostSet = true;
			}
			return _baseStarCost;
		}
		private set
		{
			AssertMutable();
			if (!HasStarCostX)
			{
				_baseStarCost = value;
				_starCostSet = true;
			}
			StarCostChanged?.Invoke();
		}
	}

	public virtual string[] AllPortraitPaths => Array.Empty<string>();

	public int LastStarsSpent
	{
		get => _lastStarsSpent;
		set
		{
			AssertMutable();
			_lastStarsSpent = value;
		}
	}

	public CombatState? CombatState
	{
		get
		{
			CardPile? pile = Pile;
			if ((pile != null && pile.IsCombatPile) || UpgradePreviewType == CardUpgradePreviewType.Combat)
			{
				return _owner?.Creature.CombatState;
			}
			return null;
		}
	}

	public RunState RunState => Owner.RunState as RunState ?? throw new InvalidOperationException("Card owner is not attached to a mutable RunState.");

	public ICardScope CardScope => CombatState ?? (ICardScope?)_owner?.Creature.CombatState ?? Owner.RunState;

	public CardPile? Pile
	{
		get
		{
			if (!HasOwner)
			{
				return null;
			}
			return _owner?.Piles.FirstOrDefault((CardPile p) => p.Cards.Contains(this));
		}
	}

	protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();
	
	public virtual bool HasTurnEndInHandEffect => false;

	public override bool ShouldReceiveCombatHooks => Pile?.IsCombatPile ?? false;
	
	protected virtual HashSet<CardTag> CanonicalTags => new HashSet<CardTag>();

	public virtual IEnumerable<CardTag> Tags => _tags ??= CanonicalTags;

	public virtual IEnumerable<CardKeyword> CanonicalKeywords => Array.Empty<CardKeyword>();

	public IReadOnlySet<CardKeyword> Keywords
	{
		get
		{
			if (_keywords != null)
			{
				return _keywords;
			}
			_keywords = new HashSet<CardKeyword>();
			_keywords.UnionWith(CanonicalKeywords);
			return _keywords;
		}
	}
		
	public virtual bool CanBeGeneratedInCombat => true;

	public virtual bool CanBeGeneratedByModifiers => true;

	public virtual OrbEvokeType OrbEvokeType => OrbEvokeType.None;

	public virtual bool GainsBlock => false;

	public event Action? AfflictionChanged;

	public event Action? EnchantmentChanged;

	public event Action? EnergyCostChanged;

	public event Action? KeywordsChanged;

	public event Action? ReplayCountChanged;

	public event Action? Played;

	public event Action? Drawn;

	public event Action? StarCostChanged;

	public event Action? Upgraded;

	public event Action? Forged;
	
	public virtual bool IsBasicStrikeOrDefend
	{
		get
		{
			if (Rarity != CardRarity.Basic)
			{
				return false;
			}
			if (Tags.Contains(CardTag.Strike))
			{
				return true;
			}
			if (Tags.Contains(CardTag.Defend))
			{
				return true;
			}
			return false;
		}
	}

	public CardModel? CloneOf => _cloneOf;

	public virtual int CanonicalStarCost => -1;

	public DynamicVarSet DynamicVars
	{
		get
		{
			if (_dynamicVars != null)
			{
				return _dynamicVars;
			}
			_dynamicVars = new DynamicVarSet(CanonicalVars);
			_dynamicVars.InitializeWithOwner(this);
			return _dynamicVars;
		}
	}

	protected virtual bool ShouldGlowGoldInternal => false;
	
	protected virtual bool ShouldGlowRedInternal => false;


	protected CardModel()
	{
		ResetCostsForTurn();
	}

	protected CardModel(int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType)
	{
		CanonicalEnergyCost = canonicalEnergyCost;
		Type = type;
		Rarity = rarity;
		TargetType = targetType;
		ResetCostsForTurn();
	}

	public int GetEnergyCostToPay()
	{
		if (IsFreeToPlay)
		{
			return 0;
		}
		return Math.Max(0, EnergyCost.GetWithModifiers(CostModifiers.All));
	}

	public int GetStarCostToPay()
	{
		if (IsFreeToPlay)
		{
			return 0;
		}
		if (CurrentStarCost < 0)
		{
			return -1;
		}
		return Math.Max(0, GetStarCostWithModifiers());
	}

	public bool RequiresTarget => TargetType.IsSingleTarget();

	public virtual bool CanPlay(Creature? target)
	{
		return CanPlayTargeting(target);
	}

	public bool CanPlayTargeting(Creature? target)
	{
		if (!IsValidTarget(target))
		{
			return false;
		}
		return CanPlay();
	}

	public bool CanPlay()
	{
		return CanPlay(out _, out _);
	}

	public bool TryManualPlay(Creature? target)
	{
		if (CanPlayTargeting(target))
		{
			EnqueueManualPlay(target);
			return true;
		}
		return false;
	}

	private void EnqueueManualPlay(Creature? target)
	{
		OnEnqueuePlayVfx(target);
		RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new PlayCardAction(this, target));
	}

	public bool CanAffordCosts()
	{
		if (!HasOwner)
		{
			return false;
		}
		return Owner.PlayerCombatState != null && Owner.PlayerCombatState.HasEnoughResourcesFor(this, out _);
	}

	public void ValidateTarget(Creature? target)
	{
		switch (TargetType)
		{
		case TargetType.None:
		case TargetType.AllEnemies:
		case TargetType.AllAllies:
		case TargetType.RandomEnemy:
		case TargetType.Osty:
			if (target != null)
			{
				throw new InvalidOperationException($"Card '{Id}' does not accept a single explicit target for TargetType.{TargetType}.");
			}
			return;
		case TargetType.TargetedNoCreature:
			return;
		default:
			AssertValidForTargetedCard(target);
			if (!CanTarget(target))
			{
				throw new InvalidOperationException($"Target creature is not valid for card '{Id}' with TargetType.{TargetType}.");
			}
			return;
		}
	}

	public bool CanTarget(Creature creature)
	{
		if (!HasOwner)
		{
			return false;
		}
		return TargetType switch
		{
			TargetType.Self => ReferenceEquals(creature, Owner.Creature),
			TargetType.AnyEnemy => creature.Side != Owner.Creature.Side,
			TargetType.AnyPlayer => creature.Side == Owner.Creature.Side,
			TargetType.AnyAlly => creature.Side == Owner.Creature.Side && !ReferenceEquals(creature, Owner.Creature),
			_ => true
		};
	}

	public virtual bool IsValidTarget(Creature? target)
	{
		if (target == null)
		{
			if (TargetType != TargetType.AnyEnemy)
			{
				return TargetType != TargetType.AnyAlly;
			}
			return false;
		}
		if (!target.IsAlive)
		{
			return false;
		}
		if (TargetType == TargetType.AnyEnemy)
		{
			return target.Side != Owner.Creature.Side;
		}
		if (TargetType == TargetType.AnyAlly)
		{
			return target.Side == Owner.Creature.Side;
		}
		return false;
	}

	public void ResetCostsForTurn()
	{
		IsFreeToPlay = false;
		EnergyCostForTurn = CanonicalEnergyCost;
		StarCostForTurn = CanonicalStarCost;
		_exhaustOnNextPlay = false;
		_hasSingleTurnRetain = false;
		_hasSingleTurnSly = false;
	}

	public void SetEnergyCostForTurn(int cost)
	{
		EnergyCostForTurn = Math.Max(0, cost);
		EnergyCost.SetThisTurnOrUntilPlayed(EnergyCostForTurn);
		InvokeEnergyCostChanged();
	}

	protected void NeverEverCallThisOutsideOfTests_ClearOwner()
	{
		_owner = null;
	}

	protected void MockSetEnergyCost(CardEnergyCost energyCost)
	{
		_energyCost = energyCost ?? throw new ArgumentNullException(nameof(energyCost));
		EnergyCostForTurn = energyCost.Canonical;
	}

	public void SetStarCostForTurn(int cost)
	{
		StarCostForTurn = cost;
		SetStarCostThisTurn(cost);
	}

	public void SetFreeToPlayThisTurn()
	{
		SetToFreeThisTurn();
	}

	public void SetToFreeThisTurn()
	{
		EnergyCost.SetThisTurnOrUntilPlayed(0);
		SetStarCostThisTurn(0);
	}

	public void SetToFreeThisCombat()
	{
		EnergyCost.SetThisCombat(0);
		SetStarCostThisCombat(0);
	}

	public void SetStarCostUntilPlayed(int cost)
	{
		AddTemporaryStarCost(TemporaryCardCost.UntilPlayed(cost));
	}

	public void SetStarCostThisTurn(int cost)
	{
		AddTemporaryStarCost(TemporaryCardCost.ThisTurn(cost));
	}

	public void SetStarCostThisCombat(int cost)
	{
		AddTemporaryStarCost(TemporaryCardCost.ThisCombat(cost));
	}

	public int GetStarCostThisCombat()
	{
		return _temporaryStarCosts.FirstOrDefault((TemporaryCardCost cost) => cost != null && !cost.ClearsWhenTurnEnds && !cost.ClearsWhenCardIsPlayed)?.Cost ?? BaseStarCost;
	}

	private void AddTemporaryStarCost(TemporaryCardCost cost)
	{
		AssertMutable();
		_temporaryStarCosts.Add(cost);
		StarCostChanged?.Invoke();
	}

	protected void UpgradeStarCostBy(int addend)
	{
		if (HasStarCostX)
		{
			throw new InvalidOperationException("UpgradeStarCostBy called on " + base.Id.Entry + " which has star cost X.");
		}
		if (addend == 0)
		{
			return;
		}
		int baseStarCost = BaseStarCost;
		BaseStarCost += addend;
		_wasStarCostJustUpgraded = true;
		if (BaseStarCost < baseStarCost)
		{
			_temporaryStarCosts.RemoveAll((TemporaryCardCost c) => c.Cost > BaseStarCost);
		}
	}

	public void GiveSingleTurnRetain()
	{
		AssertMutable();
		_hasSingleTurnRetain = true;
	}

	public void GiveSingleTurnSly()
	{
		AssertMutable();
		_hasSingleTurnSly = true;
	}

	public void SetCurrentPile(PileType pileType)
	{
		CurrentPileType = pileType;
	}

	public void RemoveFromCurrentPile(bool silent = false)
	{
		AssertMutable();
		CardPile? pile = Pile;
		CurrentPileType = PileType.None;
		pile?.RemoveInternal(this, silent);
	}

	public void MarkRemovedFromState()
	{
		HasBeenRemovedFromState = true;
		CurrentPileType = PileType.None;
	}

	public void RemoveFromState()
	{
		RemoveFromCurrentPile();
		HasBeenRemovedFromState = true;
		CurrentPileType = PileType.None;
	}

	public SerializableCard ToSerializable()
	{
		AssertMutable();
		return new SerializableCard
		{
			Id = base.Id,
			CurrentUpgradeLevel = CurrentUpgradeLevel,
			Props = SavedProperties.From(this),
			Enchantment = Enchantment?.ToSerializable(),
			FloorAddedToDeck = FloorAddedToDeck
		};
	}

	public mySerializableCard ToKernelSerializable()
	{
		return SaveState();
	}

	public virtual mySerializableCard SaveState()
	{
		mySerializableCard save = new mySerializableCard
		{
			Id = ContentId,
			CurrentUpgradeLevel = CurrentUpgradeLevel,
			FloorAddedToDeck = FloorAddedToDeck,
			EnergyCostForTurn = EnergyCostForTurn,
			StarCostForTurn = StarCostForTurn,
			IsFreeToPlay = IsFreeToPlay,
			ExhaustOnPlay = ExhaustOnPlay,
			Retain = Retain,
			Ethereal = Ethereal,
			HasBeenRemovedFromState = HasBeenRemovedFromState,
			CurrentPileType = CurrentPileType
		};
		WriteCustomState(save.State);
		save.State[SavedKeywordsKey] = string.Join(",", Keywords.Select((CardKeyword k) => k.ToString()).OrderBy((string k) => k, StringComparer.Ordinal));
		return save;
	}

	public SerializableCard ToRunSerializable()
	{
		return new SerializableCard
		{
			Id = Id,
			CurrentUpgradeLevel = CurrentUpgradeLevel,
			FloorAddedToDeck = FloorAddedToDeck
		};
	}

	public (int, int) SpendResources()
	{
		int energy = Owner.PlayerCombatState.Energy;
		int energyToSpend = EnergyCost.GetAmountToSpend();
		int starsToSpend = Math.Max(0, GetStarCostWithModifiers());
		if (energyToSpend > energy && CombatState != null && Hook.ShouldPayExcessEnergyCostWithStars(CombatState, Owner))
		{
			starsToSpend += (energyToSpend - energy) * 2;
			energyToSpend = energy;
		}
		SpendEnergy(energyToSpend);
		SpendStars(starsToSpend);
		return (energyToSpend, starsToSpend);
	}

	public void AddKeyword(CardKeyword keyword)
	{
		AssertMutable();
		_keywords ??= new HashSet<CardKeyword>(CanonicalKeywords);
		_keywords.Add(keyword);
		KeywordsChanged?.Invoke();
	}

	public void RemoveKeyword(CardKeyword keyword)
	{
		AssertMutable();
		_keywords ??= new HashSet<CardKeyword>(CanonicalKeywords);
		_keywords.Remove(keyword);
		KeywordsChanged?.Invoke();
	}

	public void Upgrade()
	{
		if (!IsUpgradable)
		{
			throw new InvalidOperationException($"Card '{Id}' can no longer be upgraded.");
		}
		UpgradeInternal();
		FinalizeUpgradeInternal();
	}

	protected virtual void OnUpgrade()
	{
	}

	public void Use(Creature? target = null)
	{
		Use(choiceContext: null, target);
	}

	public void Use(PlayerChoiceContext? choiceContext, Creature? target)
	{
		if (!HasOwner)
		{
			throw new InvalidOperationException($"Card '{Id}' cannot be used before an owner is assigned.");
		}
		if (HasBeenRemovedFromState)
		{
			throw new InvalidOperationException($"Card '{Id}' has already been removed from state.");
		}
		if (!CanAffordCosts())
		{
			throw new InvalidOperationException($"Card '{Id}' does not have enough resources to be played.");
		}
		ValidateTarget(target);
		CurrentTarget = target;
		PayCosts();
		BeforePlayed(choiceContext, target);
		OnPlay(choiceContext, new CardPlay
		{
			Card = null!,
			Target = target,
			ResultPile = GetResultPileType(),
			Resources = new ResourceInfo
			{
				EnergySpent = 0,
				EnergyValue = 0,
				StarsSpent = 0,
				StarValue = 0
			},
			IsAutoPlay = false,
			PlayIndex = 0,
			PlayCount = 1
		});
		AfterPlayed(choiceContext, target);
		ResolveAfterPlayDestination();
		CurrentTarget = null;
	}

	protected virtual void PayCosts()
	{
		if (!HasOwner || Owner.PlayerCombatState == null)
		{
			return;
		}
		SpendResources();
	}

	private void SpendEnergy(int amount)
	{
		if (EnergyCost.CostsX)
		{
			EnergyCost.CapturedXValue = amount;
		}
		if (amount > 0)
		{
			if (CombatState != null)
			{
				CombatManager.Instance.History.EnergySpent(CombatState, amount, Owner);
			}
			Owner.PlayerCombatState.LoseEnergy(Math.Max(0, amount));
		}
		if (CombatState != null)
		{
			Hook.AfterEnergySpent(CombatState, this, amount);
		}
	}

	private void SpendStars(int amount)
	{
		LastStarsSpent = amount;
		if (amount > 0)
		{
			Owner.PlayerCombatState.LoseStars(amount);
			if (Owner.Creature.CombatState != null)
			{
				Hook.AfterStarsSpent(Owner.Creature.CombatState, amount, Owner);
			}
		}
	}

	protected virtual void ResolveAfterPlayDestination()
	{
		PileType resultPileType = GetResultPileType();
		if (resultPileType == PileType.None)
		{
			// Power牌和IsDupe牌：从战斗中完全移除
			if (HasOwner && Owner.PlayerCombatState != null && !HasBeenRemovedFromState)
			{
				CardPileCmd.RemoveFromCombat(this);
			}
			return;
		}
		PileType destinationPileType = resultPileType;
		if (HasOwner && Owner.PlayerCombatState != null && destinationPileType.IsCombatPile() && !HasBeenRemovedFromState)
		{
			CardPileCmd.Add(this, destinationPileType);
			return;
		}
		CurrentPileType = destinationPileType;
	}

	protected virtual void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return;
	}

	public virtual void AfterCreated()
	{
		return;
	}

	protected virtual void AfterDeserialized()
	{
	}

	public virtual void BeforePlayed(PlayerChoiceContext? choiceContext, Creature? target)
	{
		return;
	}

	public virtual void AfterPlayed(PlayerChoiceContext? choiceContext, Creature? target)
	{
		return;
	}

	public virtual bool CostsEnergyOrStars(bool includeGlobalModifiers)
	{
		CostModifiers modifiers = includeGlobalModifiers ? CostModifiers.All : CostModifiers.Local;
		if (EnergyCost.CostsX || EnergyCost.GetWithModifiers(modifiers) > 0)
		{
			return true;
		}
		if (HasStarCostX)
		{
			return true;
		}
		return CurrentStarCost > 0;
	}

	public virtual int ResolveEnergyXValue()
	{
		return EnergyCost.CostsX ? EnergyCost.GetAmountToSpend() : EnergyCost.GetResolved();
	}

	public int GetEnchantedReplayCount()
	{
		return Enchantment?.EnchantPlayCount(BaseReplayCount) ?? BaseReplayCount;
	}

	public virtual int ResolveStarXValue()
	{
		return HasStarCostX ? LastStarsSpent : Math.Max(0, GetStarCostWithModifiers());
	}

	public int GetStarCostWithModifiers()
	{
		if (HasStarCostX)
		{
			return Owner.PlayerCombatState?.Stars ?? 0;
		}
		CardPile? pile = Pile;
		if (pile != null && pile.IsCombatPile && CombatState != null)
		{
			return (int)Hook.ModifyStarCost(CombatState, this, CurrentStarCost);
		}
		return CurrentStarCost;
	}

	protected virtual void AddExtraArgsToDescription(LocString description)
	{
	}

	public virtual bool CanPlay(out UnplayableReason reason, out AbstractModel? preventer)
	{
		reason = UnplayableReason.None;
		CombatState? combatState = CombatState ?? _owner?.Creature.CombatState;
		if (combatState == null || !HasOwner || Owner.PlayerCombatState == null)
		{
			preventer = null;
			return false;
		}
		if (HasBeenRemovedFromState)
		{
			reason |= UnplayableReason.BlockedByHook;
		}
		if (Keywords.Contains(CardKeyword.Unplayable))
		{
			reason |= UnplayableReason.HasUnplayableKeyword;
		}
		if (!Owner.PlayerCombatState.HasEnoughResourcesFor(this, out UnplayableReason resourceReason))
		{
			reason |= resourceReason;
		}
		if (TargetType == TargetType.AnyAlly && combatState.PlayerCreatures.Count((Creature c) => c.IsAlive) <= 1)
		{
			reason |= UnplayableReason.NoLivingAllies;
		}
		if (!Hook.ShouldPlay(combatState, this, out preventer, AutoPlayType.None))
		{
			reason |= UnplayableReason.BlockedByHook;
		}
		if (!IsPlayable)
		{
			reason |= UnplayableReason.BlockedByCardLogic;
		}
		return reason == UnplayableReason.None;
	}

	public void InvokeEnergyCostChanged()
	{
		EnergyCostChanged?.Invoke();
	}

	public void InvokeDrawn()
	{
		Drawn?.Invoke();
	}

	public virtual void UpdateDynamicVarPreview()
	{
	}

	public virtual void OnEnqueuePlayVfx(Creature? target)
	{
		return;
	}

	public virtual CardModel ToMutable()
	{
		if (IsMutable)
		{
			return this;
		}
		return (CardModel)MutableClone();
	}

	protected override void DeepCloneFields()
	{
		HashSet<CardKeyword> keywords = new HashSet<CardKeyword>();
		foreach (CardKeyword keyword in Keywords)
		{
			keywords.Add(keyword);
		}
		_keywords = keywords;
		_dynamicVars = DynamicVars.Clone(this);
		_energyCost = _energyCost?.Clone(this);
		_temporaryStarCosts = _temporaryStarCosts.ToList();
		if (Enchantment != null)
		{
			EnchantmentModel enchantment = (EnchantmentModel)Enchantment.ClonePreservingMutability();
			_enchantment = null;
			EnchantInternal(enchantment, enchantment.Amount);
		}
		if (Affliction != null)
		{
			AfflictionModel affliction = (AfflictionModel)Affliction.ClonePreservingMutability();
			_affliction = null;
			AfflictInternal(affliction, affliction.Amount);
		}
	}

	protected override void AfterCloned()
	{
		base.AfterCloned();
		if (_canonicalInstance == null)
		{
			_canonicalInstance = ModelDb.GetById<CardModel>(base.Id);
		}
		CurrentTarget = null;
		DeckVersion = null;
		HasBeenRemovedFromState = false;
		AfflictionChanged = null;
		Drawn = null;
		EnchantmentChanged = null;
		EnergyCostChanged = null;
		Forged = null;
		KeywordsChanged = null;
		Played = null;
		ReplayCountChanged = null;
		StarCostChanged = null;
		Upgraded = null;
	}

	public CardModel CreateClone()
	{
		if (Pile != null && !Pile.Type.IsCombatPile())
		{
			throw new InvalidOperationException("Cannot create a clone of a card that is not in a combat pile.");
		}
		AssertMutable();
		CardModel cardModel = CardScope.CloneCard(this);
		cardModel._cloneOf = this;
		return cardModel;
	}

	public CardModel CreateDupe()
	{
		if (IsDupe)
		{
			return DupeOf?.CreateDupe() ?? throw new InvalidOperationException("A dupe card is missing its source card.");
		}
		AssertMutable();
		CardModel dupe = CreateClone();
		dupe.IsDupe = true;
		dupe.RemoveKeyword(CardKeyword.Exhaust);
		return dupe;
	}

	public virtual void OnTurnStartInHand(PlayerChoiceContext? choiceContext)
	{
		return;
	}

	public virtual void OnTurnEndInHand(PlayerChoiceContext? choiceContext)
	{
		return;
	}

	public virtual void EndOfTurnCleanup()
	{
		ExhaustOnNextPlay = false;
		IsFreeToPlay = false;
		_hasSingleTurnRetain = false;
		_hasSingleTurnSly = false;
		if (EnergyCost.EndOfTurnCleanup())
		{
			InvokeEnergyCostChanged();
		}
		if (_temporaryStarCosts.RemoveAll((TemporaryCardCost c) => c.ClearsWhenTurnEnds) > 0)
		{
			StarCostChanged?.Invoke();
		}
	}

	public virtual void AfterDrawn(PlayerChoiceContext? choiceContext, bool fromHandDraw)
	{
		return;
	}

	public virtual void AfterDiscarded(PlayerChoiceContext? choiceContext)
	{
		return;
	}

	public virtual void AfterExhausted(PlayerChoiceContext? choiceContext)
	{
		return;
	}

	public virtual void AfterChangedPiles(PileType oldPileType)
	{
		return;
	}

	public virtual CardModel OnChosen()
	{
		if (this is MegaCrit.Sts2.Core.Models.Monsters.KnowledgeDemon.IChoosable choosable)
		{
			choosable.OnChosen();
		}
		return this;
	}

	public virtual Task OnPlayWrapper(PlayerChoiceContext choiceContext, Creature? target, bool isAutoPlay, ResourceInfo resources, bool skipCardPileVisuals = false)
	{
		CurrentTarget = target;
		try
		{
			if (isAutoPlay)
			{
				CardPileCmd.Add(this, PileType.Play, CardPilePosition.Bottom, null, skipCardPileVisuals);
			}
			CombatState? cs = CombatState;
			PileType resultPileType = GetResultPileType();
			CardPilePosition resultPilePosition = CardPilePosition.Bottom;
			if (cs != null)
			{
				(resultPileType, resultPilePosition) = Hook.ModifyCardPlayResultPileTypeAndPosition(cs, this, isAutoPlay, resources, resultPileType, resultPilePosition, out IEnumerable<AbstractModel> resultModifiers);
				foreach (AbstractModel modifier in resultModifiers)
				{
					modifier.AfterModifyingCardPlayResultPileOrPosition(this, resultPileType, resultPilePosition);
				}
			}
			int playCount = GetEnchantedReplayCount() + 1;
			if (cs != null)
			{
				playCount = Hook.ModifyCardPlayCount(cs, this, playCount, target, out List<AbstractModel> modifyingModels);
				Hook.AfterModifyingCardPlayCount(cs, this, modifyingModels);
			}
			for (int i = 0; i < playCount; i++)
			{
				CardPlay cardPlay = new CardPlay
				{
					Card = this,
					Target = target,
					ResultPile = resultPileType,
					Resources = resources,
					IsAutoPlay = isAutoPlay,
					PlayIndex = i,
					PlayCount = playCount
				};
				if (cs != null)
				{
					Hook.BeforeCardPlayed(cs, cardPlay);
					CombatManager.Instance.History.CardPlayStarted(cs, cardPlay);
				}
				OnPlay(choiceContext, cardPlay);
				if (Owner.Creature.IsDead)
				{
					return Task.CompletedTask;
				}
				InvokeExecutionFinished();
				if (Enchantment != null)
				{
					Enchantment.OnPlay(choiceContext, cardPlay).GetAwaiter().GetResult();
					if (Owner.Creature.IsDead)
					{
						return Task.CompletedTask;
					}
					Enchantment.InvokeExecutionFinished();
				}
				if (Affliction != null)
				{
					AfflictionModel affliction = Affliction;
					affliction.OnPlay(choiceContext, target).GetAwaiter().GetResult();
					if (Owner.Creature.IsDead)
					{
						return Task.CompletedTask;
					}
					affliction.InvokeExecutionFinished();
				}
				if (cs != null && CombatManager.Instance.IsInProgress)
				{
					CombatManager.Instance.History.CardPlayFinished(cs, cardPlay);
					Hook.AfterCardPlayed(cs, choiceContext, cardPlay);
				}
			}
			CardPile? pile = Pile;
			if (pile != null && pile.Type == PileType.Play)
			{
				switch (resultPileType)
				{
				case PileType.None:
					CardPileCmd.RemoveFromCombat(this, skipCardPileVisuals);
					break;
				case PileType.Exhaust:
					CardCmd.Exhaust(choiceContext, this, causedByEthereal: false, skipCardPileVisuals);
					break;
				default:
					CardPileCmd.Add(this, resultPileType, resultPilePosition, null, skipCardPileVisuals);
					break;
				}
			}
			if (EnergyCost.AfterCardPlayedCleanup())
			{
				EnergyCostChanged?.Invoke();
			}
			if (_temporaryStarCosts.RemoveAll((TemporaryCardCost c) => c.ClearsWhenCardIsPlayed) > 0)
			{
				StarCostChanged?.Invoke();
			}
			IsFreeToPlay = false;
			Played?.Invoke();
		}
		finally
		{
			CurrentTarget = null;
		}
		return Task.CompletedTask;
	}

	public virtual Task MoveToResultPileWithoutPlaying(PlayerChoiceContext choiceContext)
	{
		CardPile? pile = Pile;
		if (pile != null && pile.Type == PileType.Play)
		{
			if (IsDupe)
			{
				CardPileCmd.RemoveFromCombat(this);
			}
			else if (ExhaustOnNextPlay || Keywords.Contains(CardKeyword.Exhaust))
			{
				CardCmd.Exhaust(choiceContext, this);
			}
			else
			{
				CardPileCmd.Add(this, PileType.Discard);
			}
		}
		return Task.CompletedTask;
	}

	public void UpgradeInternal()
	{
		AssertMutable();
		CurrentUpgradeLevel++;
		OnUpgrade();
		DynamicVars.RecalculateForUpgradeOrEnchant();
		Upgraded?.Invoke();
	}

	public void FinalizeUpgradeInternal()
	{
		DynamicVars.FinalizeUpgrade();
		EnergyCost.FinalizeUpgrade();
		_wasStarCostJustUpgraded = false;
	}

	public void DowngradeInternal()
	{
		AssertMutable();
		CurrentUpgradeLevel = 0;
		CardModel cardModel = ModelDb.GetById<CardModel>(base.Id).ToMutable();
		_dynamicVars = cardModel.DynamicVars.Clone(this);
		EnergyCost.ResetForDowngrade();
		_baseStarCost = cardModel.CanonicalStarCost;
		_keywords = cardModel.Keywords.ToHashSet();
		AfterDowngraded();
		Enchantment?.ModifyCard();
		Affliction?.AfterApplied();
		Upgraded?.Invoke();
	}

	public void AfterForged()
	{
		Forged?.Invoke();
	}

	public void EnchantInternal(EnchantmentModel enchantment, decimal amount)
	{
		AssertMutable();
		enchantment.AssertMutable();
		_enchantment = enchantment ?? throw new ArgumentNullException(nameof(enchantment));
		_enchantment.ApplyInternal(this, amount);
		EnchantmentChanged?.Invoke();
	}

	public void AfflictInternal(AfflictionModel affliction, decimal amount)
	{
		AssertMutable();
		affliction.AssertMutable();
		if (_affliction != null)
		{
			throw new InvalidOperationException($"Attempted to afflict card {this} that was already afflicted! This is not allowed");
		}
		_affliction = affliction ?? throw new ArgumentNullException(nameof(affliction));
		_affliction.Card = this;
		_affliction.Amount = (int)amount;
		AfflictionChanged?.Invoke();
	}

	public void ClearEnchantmentInternal()
	{
		if (_enchantment != null)
		{
			AssertMutable();
			_enchantment.ClearInternal();
			_enchantment = null;
			EnchantmentChanged?.Invoke();
		}
	}

	public void ClearAfflictionInternal()
	{
		AssertMutable();
		if (_affliction != null)
		{
			_affliction.ClearInternal();
			_affliction = null;
			Owner.PlayerCombatState.RecalculateCardValues();
			AfflictionChanged?.Invoke();
		}
	}

	public virtual void AfterTransformedFrom()
	{
	}

	public virtual void AfterTransformedTo()
	{
	}

	protected virtual PileType GetResultPileType()
	{
		if (IsDupe || Type == CardType.Power)
		{
			return PileType.None;
		}
		if (ExhaustOnNextPlay || Keywords.Contains(CardKeyword.Exhaust))
		{
			return PileType.Exhaust;
		}
		return PileType.Discard;
	}

	public virtual void LoadState(mySerializableCard save)
	{
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		if (!string.IsNullOrWhiteSpace(save.Id) && !string.Equals(save.Id, ContentId, StringComparison.Ordinal))
		{
			throw new InvalidOperationException($"Cannot load card state for '{save.Id}' into '{ContentId}'.");
		}
		CurrentUpgradeLevel = Math.Max(0, save.CurrentUpgradeLevel);
		FloorAddedToDeck = save.FloorAddedToDeck;
		EnergyCostForTurn = save.EnergyCostForTurn;
		StarCostForTurn = save.StarCostForTurn;
		IsFreeToPlay = save.IsFreeToPlay;
		if (save.State.TryGetValue(SavedKeywordsKey, out string? serializedKeywords))
		{
			_keywords = new HashSet<CardKeyword>();
			foreach (string token in serializedKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (Enum.TryParse(token, ignoreCase: false, out CardKeyword keyword))
				{
					_keywords.Add(keyword);
				}
			}
		}
		else
		{
			_keywords = new HashSet<CardKeyword>(CanonicalKeywords);
			if (save.ExhaustOnPlay)
			{
				_keywords.Add(CardKeyword.Exhaust);
			}
			if (save.Retain)
			{
				_keywords.Add(CardKeyword.Retain);
			}
			if (save.Ethereal)
			{
				_keywords.Add(CardKeyword.Ethereal);
			}
		}
		HasBeenRemovedFromState = save.HasBeenRemovedFromState;
		CurrentPileType = save.CurrentPileType;
		ReadCustomState(save.State);
	}

	public static TCard RestoreState<TCard>(mySerializableCard save)
		where TCard : CardModel, new()
	{
		TCard card = new TCard();
		card.LoadState(save);
		return card;
	}

	public static CardModel FromSerializable(mySerializableCard save)
	{
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		CardModel? card = TryCreateKernelEquivalent(save.Id);
		if (card == null)
		{
			throw new InvalidOperationException($"Unable to restore card '{save.Id}' in kernel runtime.");
		}
		card.LoadState(save);
		int upgradeLevel = card.CurrentUpgradeLevel;
		card.CurrentUpgradeLevel = 0;
		for (int i = 0; i < upgradeLevel; i++)
		{
			card.UpgradeInternal();
			card.FinalizeUpgradeInternal();
		}
		return card;
	}

	public static CardModel FromSerializable(SerializableCard save)
	{
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		if (save.Id == null)
		{
			throw new InvalidOperationException("Cannot restore a card with no id.");
		}
		CardModel card = SaveUtil.CardOrDeprecated(save.Id).ToMutable();
		save.Props?.Fill(card);
		if (save.FloorAddedToDeck.HasValue)
		{
			card.FloorAddedToDeck = save.FloorAddedToDeck.Value;
		}
		card.AfterDeserialized();
		if (!(card is MegaCrit.Sts2.Core.Models.Cards.DeprecatedCard))
		{
			if (save.Enchantment != null)
			{
				card.EnchantInternal(EnchantmentModel.FromSerializable(save.Enchantment), save.Enchantment.Amount);
				card.Enchantment?.ModifyCard();
				card.FinalizeUpgradeInternal();
			}
			for (int i = 0; i < save.CurrentUpgradeLevel; i++)
			{
				card.UpgradeInternal();
				card.FinalizeUpgradeInternal();
			}
		}
		return card;
	}

	public static CardModel FromCore(CardModel sourceCard)
	{
		if (sourceCard == null)
		{
			throw new ArgumentNullException(nameof(sourceCard));
		}
		CardModel card = TryCreateKernelEquivalent(sourceCard.ContentId) ?? new myWrappedCoreCard(sourceCard);
		CopyCommonState(card, sourceCard);
		return card;
	}

	protected virtual void WriteCustomState(Dictionary<string, string> state)
	{
	}

	protected virtual void ReadCustomState(IReadOnlyDictionary<string, string> state)
	{
	}

	private static CardModel? TryCreateKernelEquivalent(string cardId)
	{
		foreach (CardModel canonical in ModelDb.AllCards)
		{
			if (canonical.Id.Entry.Equals(cardId, StringComparison.OrdinalIgnoreCase))
			{
				return canonical.ToMutable();
			}
		}
		return null;
	}

	private static void CopyCommonState(CardModel target, CardModel sourceCard)
	{
		if (sourceCard.FloorAddedToDeck > 0)
		{
			target.FloorAddedToDeck = sourceCard.FloorAddedToDeck;
		}
		target.ExhaustOnPlay = sourceCard.Keywords.Contains(CardKeyword.Exhaust);
		target.Retain = sourceCard.Keywords.Contains(CardKeyword.Retain);
		target.Ethereal = sourceCard.Keywords.Contains(CardKeyword.Ethereal);
		for (int i = 0; i < sourceCard.CurrentUpgradeLevel && target.IsUpgradable; i++)
		{
			target.Upgrade();
		}
	}

	public static void AssertValidForTargetedCard(Creature? target)
	{
		if (target == null)
		{
			throw new ArgumentNullException(nameof(target), "Target must be present for targeted cards.");
		}
	}

	private sealed class myWrappedCoreCard : CardModel
	{
		private readonly CardModel? _sourceCardBeforeInit;

		public override string ContentId => SourceCard.ContentId;

		public override CardType Type => SourceCard.Type;

		public override CardRarity Rarity => SourceCard.Rarity;

		public override TargetType TargetType => SourceCard.TargetType;

		protected override bool HasEnergyCostX => _sourceCardBeforeInit?.EnergyCost.CostsX ?? false;

		public override bool HasStarCostX => _sourceCardBeforeInit?.HasStarCostX ?? false;

		private CardModel SourceCard => _sourceCardBeforeInit ?? throw new InvalidOperationException("Wrapped core card was not initialized.");

		public myWrappedCoreCard(CardModel sourceCard)
		{
			_sourceCardBeforeInit = sourceCard ?? throw new ArgumentNullException(nameof(sourceCard));
			ResetCostsForTurn();
		}
	}

	private sealed class myWrappedLegacyCoreCard : CardModel
	{
		private readonly MegaCrit.Sts2.Core.CardModel _sourceCard;

		public override string ContentId => _sourceCard.Id.Entry;

		public override CardType Type => _sourceCard.Type;

		public override CardRarity Rarity => _sourceCard.Rarity;

		public override TargetType TargetType => _sourceCard.TargetType;

		protected override bool HasEnergyCostX => _sourceCard.EnergyCost.CostsX;

		protected override int CanonicalEnergyCost => _sourceCard.EnergyCost.Canonical;

		public override bool HasStarCostX => _sourceCard.HasStarCostX;

		public override bool CanBeGeneratedInCombat => _sourceCard.CanBeGeneratedInCombat;

		public override bool CanBeGeneratedByModifiers => _sourceCard.CanBeGeneratedByModifiers;

		public override CardMultiplayerConstraint MultiplayerConstraint => _sourceCard.MultiplayerConstraint;

		public myWrappedLegacyCoreCard(MegaCrit.Sts2.Core.CardModel sourceCard)
		{
			_sourceCard = sourceCard ?? throw new ArgumentNullException(nameof(sourceCard));
			ResetCostsForTurn();
		}
	}

	protected virtual void AfterDowngraded()
	{
	}
}

public sealed class mySerializableCard : IPacketSerializable
{
	public string Id { get; set; } = string.Empty;

	public int CurrentUpgradeLevel { get; set; }

	public int FloorAddedToDeck { get; set; }

	public int EnergyCostForTurn { get; set; }

	public int StarCostForTurn { get; set; } = -1;

	public bool IsFreeToPlay { get; set; }

	public bool ExhaustOnPlay { get; set; }

	public bool Retain { get; set; }

	public bool Ethereal { get; set; }

	public bool HasBeenRemovedFromState { get; set; }

	public PileType CurrentPileType { get; set; } = PileType.None;

	public Dictionary<string, string> State { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

	public SerializableCard ToRunSerializable()
	{
		return new SerializableCard
		{
			Id = string.IsNullOrWhiteSpace(Id) ? null : new ModelId("CARD", Id),
			CurrentUpgradeLevel = CurrentUpgradeLevel,
			FloorAddedToDeck = FloorAddedToDeck
		};
	}

	public static implicit operator SerializableCard(mySerializableCard save)
	{
		return save?.ToRunSerializable() ?? new SerializableCard();
	}

	public static implicit operator mySerializableCard(SerializableCard save)
	{
		return new mySerializableCard
		{
			Id = save?.Id?.Entry ?? string.Empty,
			CurrentUpgradeLevel = save?.CurrentUpgradeLevel ?? 0,
			FloorAddedToDeck = save?.FloorAddedToDeck ?? 0
		};
	}

	public void Serialize(PacketWriter writer)
	{
		writer.WriteString(Id);
		writer.WriteInt(CurrentUpgradeLevel);
		writer.WriteInt(FloorAddedToDeck);
		writer.WriteInt(EnergyCostForTurn);
		writer.WriteInt(StarCostForTurn);
		writer.WriteBool(IsFreeToPlay);
		writer.WriteBool(ExhaustOnPlay);
		writer.WriteBool(Retain);
		writer.WriteBool(Ethereal);
		writer.WriteBool(HasBeenRemovedFromState);
		writer.WriteInt((int)CurrentPileType);
		writer.WriteInt(State.Count);
		foreach (KeyValuePair<string, string> pair in State.OrderBy((KeyValuePair<string, string> kvp) => kvp.Key, StringComparer.Ordinal))
		{
			writer.WriteString(pair.Key);
			writer.WriteString(pair.Value);
		}
	}

	public void Deserialize(PacketReader reader)
	{
		Id = reader.ReadString();
		CurrentUpgradeLevel = reader.ReadInt();
		FloorAddedToDeck = reader.ReadInt();
		EnergyCostForTurn = reader.ReadInt();
		StarCostForTurn = reader.ReadInt();
		IsFreeToPlay = reader.ReadBool();
		ExhaustOnPlay = reader.ReadBool();
		Retain = reader.ReadBool();
		Ethereal = reader.ReadBool();
		HasBeenRemovedFromState = reader.ReadBool();
		CurrentPileType = (PileType)reader.ReadInt();
		int count = reader.ReadInt();
		State = new Dictionary<string, string>(count, StringComparer.Ordinal);
		for (int i = 0; i < count; i++)
		{
			State[reader.ReadString()] = reader.ReadString();
		}
	}
}
