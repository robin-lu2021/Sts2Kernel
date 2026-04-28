using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Combat;

public class CombatStateTracker
{
	private readonly CombatManager _combatManager;

	private CombatState? _state;

	public event Action<CombatState>? CombatStateChanged;

	public CombatStateTracker(CombatManager combatManager)
	{
		_combatManager = combatManager;
		_combatManager.History.Changed += OnCombatHistoryChanged;
		_combatManager.CreaturesChanged += OnCreaturesChanged;
		_combatManager.TurnStarted += OnTurnStarted;
		_combatManager.TurnEnded += OnTurnEnded;
	}

	~CombatStateTracker()
	{
		_combatManager.History.Changed -= OnCombatHistoryChanged;
		_combatManager.CreaturesChanged -= OnCreaturesChanged;
		_combatManager.TurnStarted -= OnTurnStarted;
		_combatManager.TurnEnded -= OnTurnEnded;
	}

	public void SetState(CombatState state)
	{
		_state = state;
	}

	public void Subscribe(CardModel card)
	{
		card.AfflictionChanged += OnCardValueChanged;
		card.EnchantmentChanged += OnCardValueChanged;
		card.EnergyCostChanged += OnCardValueChanged;
		card.ReplayCountChanged += OnCardValueChanged;
		card.Played += OnCardValueChanged;
		card.Drawn += OnCardValueChanged;
		card.StarCostChanged += OnCardValueChanged;
		card.Upgraded += OnCardValueChanged;
		card.Forged += OnCardValueChanged;
	}

	public void Unsubscribe(CardModel card)
	{
		card.AfflictionChanged -= OnCardValueChanged;
		card.EnchantmentChanged -= OnCardValueChanged;
		card.EnergyCostChanged -= OnCardValueChanged;
		card.ReplayCountChanged -= OnCardValueChanged;
		card.Played -= OnCardValueChanged;
		card.Drawn -= OnCardValueChanged;
		card.StarCostChanged -= OnCardValueChanged;
		card.Upgraded -= OnCardValueChanged;
		card.Forged -= OnCardValueChanged;
	}

	public void Subscribe(CardPile pile)
	{
		pile.ContentsChanged += OnCardPileContentsChanged;
	}

	public void Unsubscribe(CardPile pile)
	{
		pile.ContentsChanged -= OnCardPileContentsChanged;
	}

	public void Subscribe(Creature creature)
	{
		creature.BlockChanged += OnCreatureValueChanged;
		creature.CurrentHpChanged += OnCreatureValueChanged;
		creature.MaxHpChanged += OnCreatureValueChanged;
		creature.PowerApplied += OnPowerAppliedOrRemoved;
		creature.PowerIncreased += OnPowerIncreased;
		creature.PowerDecreased += OnPowerDecreased;
		creature.PowerRemoved += OnPowerAppliedOrRemoved;
		creature.Died += OnCreatureChanged;
	}

	public void Unsubscribe(Creature creature)
	{
		creature.BlockChanged -= OnCreatureValueChanged;
		creature.CurrentHpChanged -= OnCreatureValueChanged;
		creature.MaxHpChanged -= OnCreatureValueChanged;
		creature.PowerApplied -= OnPowerAppliedOrRemoved;
		creature.PowerIncreased -= OnPowerIncreased;
		creature.PowerDecreased -= OnPowerDecreased;
		creature.PowerRemoved -= OnPowerAppliedOrRemoved;
		creature.Died -= OnCreatureChanged;
	}

	public void Subscribe(PlayerCombatState combatState)
	{
		combatState.EnergyChanged += OnPlayerCombatStateValueChanged;
		combatState.StarsChanged += OnPlayerCombatStateValueChanged;
	}

	public void Unsubscribe(PlayerCombatState combatState)
	{
		combatState.EnergyChanged -= OnPlayerCombatStateValueChanged;
		combatState.StarsChanged -= OnPlayerCombatStateValueChanged;
	}

	private void OnCardPileContentsChanged()
	{
		NotifyCombatStateChanged("OnCardPileContentsChanged");
	}

	private void OnCardValueChanged()
	{
		NotifyCombatStateChanged("OnCardValueChanged");
	}

	private void OnCombatHistoryChanged()
	{
		NotifyCombatStateChanged("OnCombatHistoryChanged");
	}

	private void OnCreatureValueChanged(int _, int __)
	{
		NotifyCombatStateChanged("OnCreatureValueChanged");
	}

	private void OnCreaturesChanged(CombatState _)
	{
		NotifyCombatStateChanged("OnCreatureChanged");
	}

	private void OnCreatureChanged(Creature _)
	{
		NotifyCombatStateChanged("OnCreaturesChanged");
	}

	private void OnPlayerCombatStateValueChanged(int _, int __)
	{
		NotifyCombatStateChanged("OnPlayerCombatStateValueChanged");
	}

	private void OnPowerAppliedOrRemoved(PowerModel _)
	{
		NotifyCombatStateChanged("OnPowerAppliedOrRemoved");
	}

	private void OnPowerDecreased(PowerModel _, bool __)
	{
		NotifyCombatStateChanged("OnPowerDecreased");
	}

	private void OnPowerIncreased(PowerModel _, int __, bool ___)
	{
		NotifyCombatStateChanged("OnPowerIncreased");
	}

	private void OnTurnStarted(CombatState _)
	{
		NotifyCombatStateChanged("OnTurnStarted");
	}

	private void OnTurnEnded(CombatState _)
	{
		NotifyCombatStateChanged("OnTurnEnded");
	}

	private void NotifyCombatStateChanged(string caller)
	{
		if (this.CombatStateChanged != null)
		{
			throw new InvalidOperationException("Backend should not be subscribing to CombatStateChanged!");
		}
	}
}
