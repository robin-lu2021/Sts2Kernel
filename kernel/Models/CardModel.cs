using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core;

public abstract class CardModel : AbstractModel
{
	private Player? _owner;

	private DynamicVarSet? _dynamicVars;

	private bool _exhaustOnNextPlay;

	private bool _hasSingleTurnRetain;

	private bool _hasSingleTurnSly;

	private CardModel? _cloneOf;

	private bool _isDupe;

	private CardEnergyCost? _energyCost;

	private int _baseReplayCount;

	private bool _singleTurnSly;

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
			return _owner ?? throw new InvalidOperationException($"Card '{ContentId}' does not have an owner yet.");
		}
		set
		{
			AssertMutable();
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			if (_owner != null)
			{
				throw new InvalidOperationException("Card " + Id.Entry + " already has an owner.");
			}
			_owner = value;
		}
	}

	public bool HasOwner => _owner != null;

	public int FloorAddedToDeck { get; set; }

	public int CurrentUpgradeLevel { get; private set; }

	public virtual int MaxUpgradeLevel => 1;

	public bool IsUpgraded => CurrentUpgradeLevel > 0;

	public bool IsUpgradable => CurrentUpgradeLevel < MaxUpgradeLevel;

	public int EnergyCostForTurn { get; private set; }

	public int StarCostForTurn { get; private set; }

	public bool IsFreeToPlay { get; private set; }

	public bool ExhaustOnPlay { get; set; }

	public bool Retain { get; set; }

	public bool Ethereal { get; set; }

	public bool HasBeenRemovedFromState { get; internal set; }

	public PileType CurrentPileType { get; private set; } = PileType.None;

	public Creature? CurrentTarget { get; private set; }

	public CardEnergyCost EnergyCost => _energyCost ??= new CardEnergyCost(this, CanonicalEnergyCost, HasEnergyCostX);

	public int BaseReplayCount
	{
		get => _baseReplayCount;
		set
		{
			_baseReplayCount = value;
			ReplayCountChanged?.Invoke();
		}
	}

	public bool ExhaustOnNextPlay
	{
		get => _exhaustOnNextPlay;
		set => _exhaustOnNextPlay = value;
	}

	public bool ShouldRetainThisTurn => Retain || _hasSingleTurnRetain || CanonicalKeywords.Contains(CardKeyword.Retain);

	public bool IsInCombat => Pile?.IsCombatPile ?? false;

	public bool IsDupe { get; protected set; }

	public CardModel? DeckVersion
	{
		get => _deckVersion ?? (CurrentPileType == PileType.Deck ? this : null);
		set => _deckVersion = value;
	}

	public CardModel CanonicalInstance => !IsMutable ? this : _canonicalInstance ?? this;

	public bool IsClone => CloneOf != null;

	public bool IsTransformable => true;

	public virtual bool IsRemovable => true;

	public bool IsSlyThisTurn => _singleTurnSly;

	public EnchantmentModel? Enchantment => _enchantment;

	public AfflictionModel? Affliction => _affliction;

	public bool IsEnchantmentPreview => Enchantment != null;

	public IEnumerable<IHoverTip> HoverTips => Array.Empty<IHoverTip>();

	public CardUpgradePreviewType UpgradePreviewType { get; set; }

	public int CurrentStarCost => StarCostForTurn;

	public virtual int BaseStarCost => CanonicalStarCost;

	public virtual TemporaryCardCost? TemporaryStarCost => HasStarCostX || StarCostForTurn < 0 || StarCostForTurn == BaseStarCost
		? null
		: TemporaryCardCost.ThisCombat(StarCostForTurn);

	public virtual string[] AllPortraitPaths => Array.Empty<string>();

	public int LastStarsSpent { get; set; }

	public CombatState? CombatState => HasOwner ? Owner.Creature.CombatState : null;

	public RunState RunState => Owner.RunState as RunState ?? throw new InvalidOperationException("Card owner is not attached to a mutable RunState.");

	public ICardScope CardScope => CombatState != null ? CombatState : Owner.RunState;

	public CardPile? Pile
	{
		get
		{
			if (!HasOwner || CurrentPileType == PileType.None)
			{
				return null;
			}
			return CurrentPileType.GetPile(Owner);
		}
	}

	protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();
	
	public virtual bool HasTurnEndInHandEffect => false;

	public override bool ShouldReceiveCombatHooks => Pile?.IsCombatPile ?? false;
	
	protected virtual HashSet<CardTag> CanonicalTags => new HashSet<CardTag>();

	public virtual IEnumerable<CardTag> Tags => CanonicalTags;

	public virtual IEnumerable<CardKeyword> CanonicalKeywords => Array.Empty<CardKeyword>();

	public virtual IReadOnlyCollection<CardKeyword> Keywords
	{
		get
		{
			HashSet<CardKeyword> keywords = new HashSet<CardKeyword>(CanonicalKeywords);
			if (ExhaustOnPlay)
			{
				keywords.Add(CardKeyword.Exhaust);
			}
			if (Retain)
			{
				keywords.Add(CardKeyword.Retain);
			}
			if (Ethereal)
			{
				keywords.Add(CardKeyword.Ethereal);
			}
			return keywords;
		}
	}
		
	public virtual bool CanBeGeneratedInCombat => true;

	public virtual bool CanBeGeneratedByModifiers => true;

	public virtual OrbEvokeType OrbEvokeType => OrbEvokeType.None;

	public virtual bool GainsBlock => false;

	public event Action? AfflictionChanged;

	public event Action? EnchantmentChanged;

	public event Action? EnergyCostChanged;

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
		if (StarCostForTurn < 0)
		{
			return -1;
		}
		return Math.Max(0, StarCostForTurn);
	}

	public bool RequiresTarget => TargetType.IsSingleTarget();

	public virtual bool CanPlay(Creature? target = null)
	{
		if (HasBeenRemovedFromState)
		{
			return false;
		}
		if (!HasOwner)
		{
			return false;
		}
		if (!CanAffordCosts())
		{
			return false;
		}
		if (!IsPlayable)
		{
			return false;
		}
		try
		{
			ValidateTarget(target);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool CanAffordCosts()
	{
		if (!HasOwner)
		{
			return false;
		}
		if (!HasEnergyCostX && Owner.MaxEnergy < GetEnergyCostToPay())
		{
			return false;
		}
		return true;
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
		try
		{
			ValidateTarget(target);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public void ResetCostsForTurn()
	{
		IsFreeToPlay = false;
		EnergyCostForTurn = CanonicalEnergyCost;
		StarCostForTurn = CanonicalStarCost;
		_exhaustOnNextPlay = false;
		_hasSingleTurnRetain = false;
		_singleTurnSly = false;
	}

	public void SetEnergyCostForTurn(int cost)
	{
		EnergyCostForTurn = Math.Max(0, cost);
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
		StarCostChanged?.Invoke();
	}

	public void SetFreeToPlayThisTurn()
	{
		IsFreeToPlay = true;
	}

	public void SetToFreeThisCombat()
	{
		SetFreeToPlayThisTurn();
	}

	public void GiveSingleTurnRetain()
	{
		_hasSingleTurnRetain = true;
	}

	public void GiveSingleTurnSly()
	{
		_singleTurnSly = true;
	}

	public void SetCurrentPile(PileType pileType)
	{
		CurrentPileType = pileType;
	}

	public void RemoveFromCurrentPile(bool silent = false)
	{
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
		MarkRemovedFromState();
	}

	public mySerializableCard ToSerializable()
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

	public void SetToFreeThisTurn()
	{
		SetFreeToPlayThisTurn();
	}

	public void SpendResources()
	{
		PayCosts();
	}

	public void AddKeyword(CardKeyword keyword)
	{
		switch (keyword)
		{
		case CardKeyword.Exhaust:
			ExhaustOnPlay = true;
			break;
		case CardKeyword.Retain:
			Retain = true;
			break;
		case CardKeyword.Ethereal:
			Ethereal = true;
			break;
		}
	}

	public void RemoveKeyword(CardKeyword keyword)
	{
		switch (keyword)
		{
		case CardKeyword.Exhaust:
			ExhaustOnPlay = false;
			break;
		case CardKeyword.Retain:
			Retain = false;
			break;
		case CardKeyword.Ethereal:
			Ethereal = false;
			break;
		}
	}

	public void Upgrade()
	{
		if (!IsUpgradable)
		{
			throw new InvalidOperationException($"Card '{Id}' can no longer be upgraded.");
		}
		CurrentUpgradeLevel++;
		OnUpgrade();
		Upgraded?.Invoke();
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
		PlayerCombatState playerCombatState = Owner.PlayerCombatState;
		int energyToSpend = EnergyCost.GetAmountToSpend();
		int starsToSpend;
		if (EnergyCost.CostsX)
		{
			EnergyCost.CapturedXValue = energyToSpend;
		}
		if (HasStarCostX)
		{
			starsToSpend = playerCombatState.Stars;
			LastStarsSpent = starsToSpend;
		}
		else
		{
			starsToSpend = Math.Max(0, GetStarCostWithModifiers());
			LastStarsSpent = starsToSpend;
		}
		if (energyToSpend > playerCombatState.Energy && CombatState != null && Hooks.Hook.ShouldPayExcessEnergyCostWithStars(CombatState, Owner))
		{
			starsToSpend += (energyToSpend - playerCombatState.Energy) * 2;
			energyToSpend = playerCombatState.Energy;
		}
		PlayerCmd.LoseEnergy(energyToSpend, Owner);
		PlayerCmd.LoseStars(starsToSpend, Owner);
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

	public virtual int GetStarCostWithModifiers()
	{
		return GetStarCostToPay();
	}

	protected virtual void AddExtraArgsToDescription(LocString description)
	{
	}

	public virtual bool CanPlay(out UnplayableReason reason, out AbstractModel? preventer)
	{
		preventer = null;
		if (HasBeenRemovedFromState)
		{
			reason = UnplayableReason.BlockedByHook;
			return false;
		}
		if (!HasOwner)
		{
			reason = UnplayableReason.BlockedByHook;
			return false;
		}
		if (!CanAffordCosts())
		{
			reason = UnplayableReason.EnergyCostTooHigh;
			return false;
		}
		if (!IsPlayable)
		{
			reason = UnplayableReason.BlockedByHook;
			return false;
		}
		reason = UnplayableReason.None;
		return true;
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
		CardModel clone = (CardModel)MutableClone();
		clone._canonicalInstance = this;
		return clone;
	}

	public CardModel CreateClone()
	{
		if (Pile != null && !Pile.Type.IsCombatPile())
		{
			throw new InvalidOperationException("Cannot create a clone of a card that is not in a combat pile.");
		}
		// AssertMutable();
		CardModel cardModel = CardScope.CloneCard(this);
		cardModel._cloneOf = this;
		return cardModel;
	}

	public CardModel CreateDupe()
	{
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
		if (EnergyCost.EndOfTurnCleanup())
		{
			InvokeEnergyCostChanged();
		}
		_hasSingleTurnRetain = false;
		_singleTurnSly = false;
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
			CardPlay cardPlay = new CardPlay
			{
				Card = this,
				Target = target,
				ResultPile = GetResultPileType(),
				Resources = resources,
				IsAutoPlay = isAutoPlay,
				PlayIndex = 0,
				PlayCount = 1
			};
			var cs = this.CombatState;
			if (cs != null)
			{
				Hook.BeforeCardPlayed(cs, cardPlay);
			}
			OnPlay(choiceContext, cardPlay);
			if (Owner.Creature.IsDead)
			{
				return Task.CompletedTask;
			}
			InvokeExecutionFinished();
			if (Enchantment != null)
			{
				Enchantment.OnPlay(choiceContext, cardPlay);
				if (Owner.Creature.IsDead)
				{
					return Task.CompletedTask;
				}
				Enchantment.InvokeExecutionFinished();
			}
			if (Affliction != null)
			{
				Affliction.OnPlay(choiceContext, target);
				if (Owner.Creature.IsDead)
				{
					return Task.CompletedTask;
				}
				Affliction.InvokeExecutionFinished();
			}
			if (cs != null && CombatManager.Instance.IsInProgress)
			{
				CombatManager.Instance.History.CardPlayFinished(cs, cardPlay);
				Hook.AfterCardPlayed(cs, choiceContext, cardPlay);
			}
			Played?.Invoke();
			ResolveAfterPlayDestination();
		}
		finally
		{
			CurrentTarget = null;
		}
		return Task.CompletedTask;
	}

	public virtual Task MoveToResultPileWithoutPlaying(PlayerChoiceContext choiceContext)
	{
		ResolveAfterPlayDestination();
		return Task.CompletedTask;
	}

	public void UpgradeInternal()
	{
		if (IsUpgradable)
		{
			CurrentUpgradeLevel++;
			OnUpgrade();
			Upgraded?.Invoke();
		}
	}

	public void FinalizeUpgradeInternal()
	{
		InvokeEnergyCostChanged();
		Forged?.Invoke();
	}

	public void DowngradeInternal()
	{
		if (CurrentUpgradeLevel > 0)
		{
			CurrentUpgradeLevel = 0;
			AfterDowngraded();
		}
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
		ExhaustOnPlay = save.ExhaustOnPlay;
		Retain = save.Retain;
		Ethereal = save.Ethereal;
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
		CardModel? card = TryCreateKernelEquivalent(save.Id.Entry);
		if (card == null)
		{
			throw new InvalidOperationException($"Unable to restore card '{save.Id}' in kernel runtime.");
		}
		card.LoadState(new mySerializableCard
		{
			Id = save.Id.Entry,
			CurrentUpgradeLevel = save.CurrentUpgradeLevel,
			FloorAddedToDeck = save.FloorAddedToDeck ?? 0
		});
		int upgradeLevel = card.CurrentUpgradeLevel;
		card.CurrentUpgradeLevel = 0;
		for (int i = 0; i < upgradeLevel; i++)
		{
			card.UpgradeInternal();
			card.FinalizeUpgradeInternal();
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
