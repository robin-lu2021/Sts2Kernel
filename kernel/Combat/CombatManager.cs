using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Combat;

public class CombatManager
{
    private sealed record PendingLossState(CombatState State, CombatRoom Room);

    public const int baseHandDrawCount = 5;

    private readonly object _playerReadyLock = new object();

    private readonly HashSet<Player> _playersReadyToEndTurn = new HashSet<Player>();

    private readonly HashSet<Player> _playersReadyToBeginEnemyTurn = new HashSet<Player>();

    private readonly List<Player> _playersTakingExtraTurn = new List<Player>();

    private CombatState? _state;

    private PendingLossState? _pendingLoss;

    private bool _playerActionsDisabled;

    public static CombatManager Instance { get; } = new CombatManager();

    public CardModel? DebugForcedTopCardOnNextShuffle { get; private set; }

    public bool IsPaused { get; private set; }

    public bool PlayerActionsDisabled
    {
        get
        {
            return _playerActionsDisabled;
        }
        private set
        {
            if (_playerActionsDisabled != value)
            {
                _playerActionsDisabled = value;
                this.PlayerActionsDisabledChanged?.Invoke(_state);
            }
        }
    }

    public IReadOnlyList<Player> PlayersTakingExtraTurn
    {
        get
        {
            lock (_playerReadyLock)
            {
                return _playersTakingExtraTurn.ToList();
            }
        }
    }

    public bool IsPlayPhase { get; private set; }

    public bool IsEnemyTurnStarted { get; private set; }

    public bool EndingPlayerTurnPhaseTwo { get; private set; }

    public bool EndingPlayerTurnPhaseOne { get; private set; }

    public CombatStateTracker StateTracker { get; }

    public CombatHistory History { get; }

    public bool IsInProgress { get; private set; }

    public bool IsAboutToLose => _pendingLoss != null;

    public bool IsEnding
    {
        get
        {
            if (!IsInProgress)
            {
                return false;
            }
            if (_pendingLoss != null)
            {
                return true;
            }
            if (_state != null && _state.Enemies.Any((Creature e) => e != null && e.IsAlive && e.IsPrimaryEnemy))
            {
                return false;
            }
            if (Hook.ShouldStopCombatFromEnding(_state))
            {
                return false;
            }
            return true;
        }
    }

    public bool IsOverOrEnding
    {
        get
        {
            if (!IsEnding)
            {
                return !IsInProgress;
            }
            return true;
        }
    }

    public event Action<CombatState>? CombatSetUp;

    public event Action<CombatRoom>? CombatEnded;

    public event Action<CombatRoom>? CombatWon;

    public event Action<CombatState>? CreaturesChanged;

    public event Action<CombatState>? TurnStarted;

    public event Action<CombatState>? TurnEnded;

    public event Action<Player, bool>? PlayerEndedTurn;

    public event Action<Player>? PlayerUnendedTurn;

    public event Action<CombatState>? AboutToSwitchToEnemyTurn;

    public event Action<CombatState>? PlayerActionsDisabledChanged;

    public CombatState? DebugOnlyGetState()
    {
        return _state;
    }

    private CombatManager()
    {
        History = new CombatHistory();
        StateTracker = new CombatStateTracker(this);
    }

    public void SetUpCombat(CombatState state)
    {
        if (_state != null)
        {
            throw new InvalidOperationException("Make sure to reset the combat before setting up a new one.");
        }
        _state = state;
        _state.MultiplayerScalingModel?.OnCombatEntered(_state);
        StateTracker.SetState(state);
        lock (_playerReadyLock)
        {
            _playersTakingExtraTurn.Clear();
        }
        foreach (Player player in state.Players)
        {
            player.ResetCombatState();
        }
        foreach (Player player2 in state.Players)
        {
            player2.PopulateCombatState(player2.RunState.Rng.Shuffle, state);
        }
        NetCombatCardDb.Instance.StartCombat(state.Players);
        foreach (Creature creature in state.Creatures)
        {
            AddCreature(creature);
        }
        this.CombatSetUp?.Invoke(state);
    }

    public void AfterCombatRoomLoaded()
    {
        StartCombatInternal();
    }

    private void StartCombatInternal()
    {
        foreach (Creature creature in _state.Creatures)
        {
            AfterCreatureAdded(creature);
        }
        RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.NotPlayPhase);
        IsInProgress = true;
        Hook.BeforeCombatStart(_state.RunState, _state);
        StartTurn();
    }

    private void StartTurn(Action? actionDuringEnemyTurn = null)
    {
        if (!IsInProgress)
        {
            return;
        }
        bool isExtraPlayerTurn;
        List<Creature> creaturesStartingTurn;
        List<Player> playersStartingTurn;
        lock (_playerReadyLock)
        {
            isExtraPlayerTurn = _playersTakingExtraTurn.Count > 0;
            CombatState? state = _state;
            if (state != null && state.CurrentSide == CombatSide.Player && isExtraPlayerTurn)
            {
                creaturesStartingTurn = _playersTakingExtraTurn.Select((Player p) => p.Creature).ToList();
                playersStartingTurn = _playersTakingExtraTurn.ToList();
            }
            else
            {
                creaturesStartingTurn = _state?.CreaturesOnCurrentSide.ToList() ?? new List<Creature>();
                CombatState? state2 = _state;
                playersStartingTurn = ((state2 == null || state2.CurrentSide != CombatSide.Player) ? new List<Player>() : (_state?.Players.ToList() ?? new List<Player>()));
            }
        }
        foreach (Creature item in creaturesStartingTurn)
        {
            if (_state != null)
            {
                item.BeforeTurnStart(_state.RoundNumber, _state.CurrentSide);
            }
        }
        if (_state != null)
        {
            Hook.BeforeSideTurnStart(_state, _state.CurrentSide);
        }
        CombatState? state3 = _state;
        if (state3 != null && state3.CurrentSide == CombatSide.Player)
        {
            PlayerActionsDisabled = false;
            lock (_playerReadyLock)
            {
                _playersReadyToEndTurn.Clear();
                _playersReadyToBeginEnemyTurn.Clear();
            }
            if (!isExtraPlayerTurn)
            {
                foreach (Creature enemy in _state.Enemies)
                {
                    enemy.PrepareForNextTurn(_state.PlayerCreatures);
                }
            }
        }
        foreach (Creature item2 in creaturesStartingTurn)
        {
            if (_state != null)
            {
                item2.AfterTurnStart(_state.RoundNumber, _state.CurrentSide).GetAwaiter().GetResult();
            }
        }
        foreach (Creature item3 in creaturesStartingTurn)
        {
            if (_state != null)
            {
                Hook.AfterBlockCleared(_state, item3);
            }
        }
		List<(HookPlayerChoiceContext, Task)> setupPlayerTurnContext = new List<(HookPlayerChoiceContext, Task)>();
        foreach (Player item4 in playersStartingTurn)
        {
            HookPlayerChoiceContext hookPlayerChoiceContext = new HookPlayerChoiceContext(item4, LocalContext.NetId.Value, GameActionType.CombatPlayPhaseOnly);
            SetupPlayerTurn(item4, hookPlayerChoiceContext);
            Task task = Task.CompletedTask;
            hookPlayerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(task);
			setupPlayerTurnContext.Add((hookPlayerChoiceContext, task));
        }
        if (_state != null)
        {
            Hook.AfterSideTurnStart(_state, _state.CurrentSide);
        }
        CombatState? state4 = _state;
        if (state4 != null && state4.CurrentSide == CombatSide.Player)
        {
            foreach (Player item5 in playersStartingTurn)
            {
                if (item5.PlayerCombatState != null)
                {
                    HookPlayerChoiceContext hookPlayerChoiceContext2 = new HookPlayerChoiceContext(item5, LocalContext.NetId.Value, GameActionType.CombatPlayPhaseOnly);
                    Task task2 = item5.PlayerCombatState.OrbQueue.AfterTurnStart(hookPlayerChoiceContext2);
                    hookPlayerChoiceContext2.AssignTaskAndWaitForPauseOrCompletion(task2);
                }
            }
            RunManager.Instance.ChecksumTracker.GenerateChecksum("After player turn start", null);
            if (_state == null)
			{
				return;
			}
            foreach (Player player in _state.Players)
            {
                if (player.Creature.IsDead || !playersStartingTurn.Contains(player))
                {
                    Log.Info($"Setting player {player.NetId} to ready at start of turn. IsDead: {player.Creature.IsDead}. IsStartingTurn: {playersStartingTurn.Contains(player)}");
                    SetReadyToEndTurn(player, canBackOut: false);
					if (AllPlayersReadyToEndTurn())
					{
						return;
					}
                }
            }
            foreach (var item6 in playersStartingTurn.Zip(setupPlayerTurnContext))
			{
				(HookPlayerChoiceContext, Task) item = item6.Second;
				var (player, _) = item6;
				var (hookPlayerChoiceContext2, setupPlayerTurnTask) = item;
				if (_state == null)
				{
					return;
				}
				if (!player.Creature.IsDead)
				{
					Task task3 = Hook.BeforePlayPhaseStart(hookPlayerChoiceContext2, setupPlayerTurnTask, _state, player);
					hookPlayerChoiceContext2.AssignTaskAndWaitForPauseOrCompletion(task3);
				}
			}
            CheckWinCondition();
            if (IsInProgress)
            {
                RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.PlayPhase);
                IsPlayPhase = true;
                IsEnemyTurnStarted = false;
                this.TurnStarted?.Invoke(_state);
            }
        }
        else
        {
            IsEnemyTurnStarted = true;
            if (_state != null)
            {
                this.TurnStarted?.Invoke(_state);
            }
            RunManager.Instance.ChecksumTracker.GenerateChecksum("After enemy turn start", null);
            CheckWinCondition();
            if (IsInProgress)
            {
                ExecuteEnemyTurn(actionDuringEnemyTurn);
            }
        }
    }

    private void SetupPlayerTurn(Player player, HookPlayerChoiceContext playerChoiceContext)
    {
        if (player.Creature.IsDead)
        {
            return;
        }
        if (_state == null || player.PlayerCombatState == null)
        {
            Log.Warn($"Combat state is null. Assuming that the run has been cleaned up. (CombatState: {_state} PlayerCombatState: {player.PlayerCombatState})");
            return;
        }
		CombatState state = _state;
        if (Hook.ShouldPlayerResetEnergy(_state, player))
        {
            player.PlayerCombatState.ResetEnergy();
        }
        else
        {
            player.PlayerCombatState.AddMaxEnergyToCurrent();
        }
        Hook.AfterEnergyReset(_state, player);
        Hook.BeforeHandDraw(_state, player, playerChoiceContext);
        decimal handDraw = Hook.ModifyHandDraw(_state, player, 5m, out IEnumerable<AbstractModel> modifiers);
        Hook.AfterModifyingHandDraw(_state, modifiers);
        if (_state.RoundNumber == 1)
        {
            CardPile pile = PileType.Draw.GetPile(player);
            List<CardModel> list = pile.Cards.Where((CardModel c) => c.Enchantment?.ShouldStartAtBottomOfDrawPile ?? false).ToList();
            foreach (CardModel item in list)
            {
                pile.MoveToBottomInternal(item);
            }
            List<CardModel> list2 = pile.Cards.Where((CardModel c) => c.Keywords.Contains(CardKeyword.Innate)).Except(list).ToList();
            foreach (CardModel item2 in list2)
            {
                pile.MoveToTopInternal(item2);
            }
            handDraw = Math.Max(handDraw, list2.Count);
            handDraw = Math.Min(handDraw, 10m);
        }
        CardPileCmd.Draw(playerChoiceContext, handDraw, player, fromHandDraw: true);
        Hook.AfterPlayerTurnStart(_state, playerChoiceContext, player);
    }

    public void SetReadyToEndTurn(Player player, bool canBackOut, Action? actionDuringEnemyTurn = null)
    {
        lock (_playerReadyLock)
        {
			if (_playersReadyToEndTurn.Contains(player))
			{
				return;
			}
            _playersReadyToEndTurn.Add(player);
        }
        this.PlayerEndedTurn?.Invoke(player, canBackOut);
        if (AllPlayersReadyToEndTurn())
        {
			Log.Debug("All players ready to end turn");
            GameAction currentlyRunningAction = RunManager.Instance.ActionExecutor.CurrentlyRunningAction;
            if (currentlyRunningAction != null && ActionQueueSet.IsGameActionPlayerDriven(currentlyRunningAction))
            {
                WaitForActionThenEndTurn(currentlyRunningAction, actionDuringEnemyTurn);
            }
            else
            {
                AfterAllPlayersReadyToEndTurn(actionDuringEnemyTurn);
            }
        }
    }

    public void UndoReadyToEndTurn(Player player)
    {
        lock (_playerReadyLock)
        {
            _playersReadyToEndTurn.Remove(player);
        }
        if (LocalContext.IsMe(player))
        {
            PlayerActionsDisabled = false;
        }
        this.PlayerUnendedTurn?.Invoke(player);
    }

    public void OnEndedTurnLocally()
    {
        PlayerActionsDisabled = true;
    }

    public void SetReadyToBeginEnemyTurn(Player player, Action? actionDuringEnemyTurn = null)
    {
        if (!IsInProgress)
        {
            Log.Error("Trying to set player ready to begin enemy turn, but combat is over!");
        }
        bool flag;
        lock (_playerReadyLock)
        {
            _playersReadyToBeginEnemyTurn.Add(player);
            flag = _playersReadyToBeginEnemyTurn.Count == _state.Players.Count && _state.CurrentSide == CombatSide.Player;
        }
        if (flag || RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
        {
            AfterAllPlayersReadyToBeginEnemyTurn(actionDuringEnemyTurn);
        }
    }

    public bool IsPlayerReadyToEndTurn(Player player)
    {
        lock (_playerReadyLock)
        {
            return _playersReadyToEndTurn.Contains(player);
        }
    }

    public bool AllPlayersReadyToEndTurn()
    {
        bool flag;
        lock (_playerReadyLock)
        {
            flag = _playersReadyToEndTurn.Count == _state.Players.Count;
        }
        if (!RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
        {
            if (flag)
            {
                return _state.CurrentSide == CombatSide.Player;
            }
            return false;
        }
        return true;
    }

    private void EndEnemyTurn()
    {
        if (_state.CurrentSide != CombatSide.Enemy)
        {
            throw new InvalidOperationException($"EndEnemyTurn called while the current side is {_state.CurrentSide}!");
        }
        EndEnemyTurnInternal();
        CheckWinCondition();
        if (!IsEnding)
        {
            SwitchSides();
            StartTurn();
        }
    }

    public void AddCreature(Creature creature)
    {
        if (!_state.ContainsCreature(creature))
        {
            throw new InvalidOperationException("CombatState must already contain creature.");
        }
        creature.Monster?.SetUpForCombat();
        if (creature.SlotName != null)
        {
            _state.SortEnemiesBySlotName();
        }
        StateTracker.Subscribe(creature);
        this.CreaturesChanged?.Invoke(_state);
    }

    public void AfterCreatureAdded(Creature creature)
    {
        creature.AfterAddedToRoom();
        if (creature.IsEnemy && _state.CurrentSide == CombatSide.Player)
        {
            creature.Monster.RollMove(_state.Players.Select((Player p) => p.Creature));
        }
    }

    public void CheckForEmptyHand(PlayerChoiceContext choiceContext, Player player)
    {
        if (IsInProgress && !PileType.Hand.GetPile(player).Cards.Any())
        {
            Hook.AfterHandEmptied(_state, choiceContext, player);
        }
    }

    public void Reset(bool graceful)
    {
        if (graceful && _state != null)
        {
            foreach (Creature item in _state.Creatures.ToList())
            {
                item.Reset();
                RemoveCreature(item);
                _state.RemoveCreature(item);
            }
            _state = null;
        }
        _pendingLoss = null;
        DebugForcedTopCardOnNextShuffle = null;
        IsInProgress = false;
        IsPlayPhase = false;
        IsEnemyTurnStarted = false;
        History.Clear();
        RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.NotInCombat);
    }

    public void HandlePlayerDeath(Player player)
    {
        if (IsInProgress)
        {
            CardModel[] cards = new CardPile[5]
            {
                player.PlayerCombatState.Hand,
                player.PlayerCombatState.DrawPile,
                player.PlayerCombatState.DiscardPile,
                player.PlayerCombatState.ExhaustPile,
                player.PlayerCombatState.PlayPile
            }.SelectMany((CardPile p) => p.Cards).ToArray();
            CardPileCmd.RemoveFromCombat(cards);
            PlayerCmd.SetEnergy(0m, player);
            PlayerCmd.SetStars(0m, player);
        }
    }

    public void LoseCombat()
    {
        if (!(_pendingLoss != null))
        {
            _pendingLoss = new PendingLossState(_state, (CombatRoom)_state.RunState.CurrentRoom);
        }
    }

    private void ProcessPendingLoss()
    {
        if (!(_pendingLoss == null))
        {
            PendingLossState pendingLoss = _pendingLoss;
            _pendingLoss = null;
            IsInProgress = false;
            this.CombatEnded?.Invoke(pendingLoss.Room);
        }
    }

    public void EndCombatInternal()
    {
        CombatState combatState = _state;
        Player localPlayer = LocalContext.GetMe(combatState);
        IRunState runState = combatState.RunState;
        CombatRoom room = (CombatRoom)runState.CurrentRoom;
        IsInProgress = false;
        IsPlayPhase = false;
        PlayerActionsDisabled = false;
        lock (_playerReadyLock)
        {
            _playersTakingExtraTurn.Clear();
        }
        foreach (Player player in combatState.Players)
        {
            player.ReviveBeforeCombatEnd();
        }
        Hook.AfterCombatEnd(runState, combatState, room);
        History.Clear();
        room.OnCombatEnded();
        if (RunManager.Instance.NetService.Type != NetGameType.Replay)
        {
			RunManager.Instance.WriteReplay(stopRecording: true);
        }
        foreach (Player player2 in combatState.Players)
        {
            player2.AfterCombatEnd();
        }
        Hook.AfterCombatVictory(runState, combatState, room);
        if (runState.CurrentMapPointHistoryEntry != null)
        {
            runState.CurrentMapPointHistoryEntry.Rooms.Last().TurnsTaken = combatState.RoundNumber;
        }
        bool flag = runState.Map.SecondBossMapPoint != null && runState.CurrentMapCoord == runState.Map.SecondBossMapPoint.coord;
        bool flag2 = runState.Map.SecondBossMapPoint == null && runState.CurrentMapCoord == runState.Map.BossMapPoint.coord;
        if (room.RoomType == RoomType.Boss && runState.CurrentActIndex == runState.Acts.Count - 1 && (flag || flag2))
        {
            RunManager.Instance.WinTime = RunManager.Instance.RunTime;
        }
        room.MarkPreFinished();
        SaveManager.Instance.SaveRun(room, saveProgress: false);
        SaveManager.Instance.UpdateProgressAfterCombatWon(localPlayer, room);
        AchievementsHelper.CheckForDefeatedAllEnemiesAchievement(runState.Act, localPlayer);
        SaveManager.Instance.SaveProgressFile();
        if (room.RoomType == RoomType.Boss)
        {
            AchievementsHelper.AfterBossDefeated(localPlayer);
        }
        combatState.MultiplayerScalingModel?.OnCombatFinished();
		if (_state != null)
		{
			this.CombatWon?.Invoke(room);
		}
        RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.NotInCombat);
		if (_state != null)
		{
			this.CombatEnded?.Invoke(room);
		}
    }

    public void RemoveCreature(Creature creature)
    {
        if (creature.IsMonster)
        {
            creature.Monster.BeforeRemovedFromRoom();
            creature.Monster.ResetStateMachine();
        }
        StateTracker.Unsubscribe(creature);
        this.CreaturesChanged?.Invoke(_state);
    }

    public bool CheckWinCondition()
    {
        if (_pendingLoss != null)
        {
            ProcessPendingLoss();
            return true;
        }
        if (IsEnding)
        {
            EndCombatInternal();
            return true;
        }
        return false;
    }

    private void ExecuteEnemyTurn(Action? actionDuringEnemyTurn = null)
    {
        if (!IsInProgress)
        {
            return;
        }
        if (actionDuringEnemyTurn != null)
        {
            actionDuringEnemyTurn();
        }
        foreach (Creature enemy in _state.Enemies.ToList())
        {
            if (_state.ContainsCreature(enemy))
            {
                enemy.TakeTurn();
                CheckWinCondition();
                if (!IsInProgress)
                {
                    return;
                }
            }
        }
        RunManager.Instance.ChecksumTracker.GenerateChecksum("After enemy turn end", null);
        EndEnemyTurn();
    }

    private void WaitForActionThenEndTurn(GameAction action, Action? actionDuringEnemyTurn)
    {
        action.CompletionTask.GetAwaiter().GetResult();
        AfterAllPlayersReadyToEndTurn(actionDuringEnemyTurn);
    }

    private void AfterAllPlayersReadyToEndTurn(Action? actionDuringEnemyTurn = null)
    {
        EndingPlayerTurnPhaseOne = true;
        RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.EndTurnPhaseOne);
        WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction();
        EndPlayerTurnPhaseOneInternal();
        if (IsInProgress && RunManager.Instance.NetService.Type != NetGameType.Replay)
        {
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new ReadyToBeginEnemyTurnAction(LocalContext.GetMe(_state), actionDuringEnemyTurn));
        }
        EndingPlayerTurnPhaseOne = false;
    }

    private void WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction()
    {
        GameAction currentlyRunningAction = RunManager.Instance.ActionExecutor.CurrentlyRunningAction;
        TaskCompletionSource completionSource;
        if (currentlyRunningAction != null && ActionQueueSet.IsGameActionPlayerDriven(currentlyRunningAction))
        {
            completionSource = new TaskCompletionSource();
            RunManager.Instance.ActionExecutor.AfterActionExecuted += AfterActionExecuted;
            completionSource.Task.GetAwaiter().GetResult();
            RunManager.Instance.ActionExecutor.AfterActionExecuted -= AfterActionExecuted;
        }
        void AfterActionExecuted(GameAction action)
        {
            GameAction readyAction = RunManager.Instance.ActionQueueSet.GetReadyAction();
            if (readyAction == null || !ActionQueueSet.IsGameActionPlayerDriven(readyAction))
            {
                completionSource.SetResult();
            }
        }
    }

    public void EndPlayerTurnPhaseOneInternal()
    {
        if (_state == null)
		{
			return;
		}
		CombatState? state = _state;
		if (state == null || state.CurrentSide != CombatSide.Player)
        {
            throw new InvalidOperationException($"EndPlayerTurn called while the current side is {_state?.CurrentSide}!");
        }
        IsPlayPhase = false;
		if (_state != null)
		{
			Hook.BeforeTurnEnd(_state, _state.CurrentSide);
		}
        if (CheckWinCondition())
        {
            return;
        }
        List<Player> playersEndingTurn;
        lock (_playerReadyLock)
        {
            playersEndingTurn = ((_playersTakingExtraTurn.Count > 0) ? _playersTakingExtraTurn.ToList() : _state?.Players.ToList());
        }
        foreach (Player item in playersEndingTurn)
        {
			if (LocalContext.NetId.HasValue)
			{
                HookPlayerChoiceContext hookPlayerChoiceContext = new HookPlayerChoiceContext(item, LocalContext.NetId.Value, GameActionType.Combat);
                DoTurnEnd(item, hookPlayerChoiceContext);
            }
        }
        if (_state != null)
		{
            foreach (Player item2 in playersEndingTurn)
            {
                Hook.BeforeFlush(_state, item2);
            }
        }
        RunManager.Instance.ChecksumTracker.GenerateChecksum("After player turn phase one end", null);
        CheckWinCondition();
    }

    private void DoTurnEnd(Player player, PlayerChoiceContext choiceContext)
    {
        player.PlayerCombatState.OrbQueue.BeforeTurnEnd(choiceContext).GetAwaiter().GetResult();
        CardPile pile = PileType.Hand.GetPile(player);
        CardPile discardPile = PileType.Discard.GetPile(player);
        List<CardModel> turnEndCards = new List<CardModel>();
        List<CardModel> list = new List<CardModel>();
        foreach (CardModel card2 in pile.Cards)
        {
            if (card2.HasTurnEndInHandEffect)
            {
                turnEndCards.Add(card2);
            }
            else if (card2.Keywords.Contains(CardKeyword.Ethereal) && Hook.ShouldEtherealTrigger(player.Creature.CombatState, card2))
            {
                list.Add(card2);
            }
        }
        foreach (CardModel item in list)
        {
            CardCmd.Exhaust(choiceContext, item, causedByEthereal: true);
        }
        foreach (CardModel card in turnEndCards)
        {
            CardPileCmd.Add(card, PileType.Play);
            card.OnTurnEndInHand(choiceContext);
            if (card.Keywords.Contains(CardKeyword.Ethereal))
            {
                CardCmd.Exhaust(choiceContext, card, causedByEthereal: true);
            }
            else
            {
                CardPileCmd.Add(card, discardPile);
            }
        }
    }

    private void EndEnemyTurnInternal()
    {
        Hook.BeforeTurnEnd(_state, _state.CurrentSide);
        foreach (Player player in _state.Players)
        {
            player.PlayerCombatState.EndOfTurnCleanup();
        }
        Hook.AfterTurnEnd(_state, _state.CurrentSide);
    }

    private void AfterAllPlayersReadyToBeginEnemyTurn(Action? actionDuringEnemyTurn = null)
    {
        EndingPlayerTurnPhaseTwo = true;
        RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.NotPlayPhase);
        this.AboutToSwitchToEnemyTurn?.Invoke(_state);
        EndPlayerTurnPhaseTwoInternal();
        SwitchFromPlayerToEnemySide(actionDuringEnemyTurn);
        EndingPlayerTurnPhaseTwo = false;
    }

    public void EndPlayerTurnPhaseTwoInternal()
    {
        if (_state.CurrentSide != CombatSide.Player)
        {
            throw new InvalidOperationException($"EndPlayerTurnPhaseTwo called while the current side is {_state.CurrentSide}!");
        }
        List<Player> list;
        lock (_playerReadyLock)
        {
            list = ((_playersTakingExtraTurn.Count > 0) ? _playersTakingExtraTurn.ToList() : _state.Players.ToList());
        }
        foreach (Player player in list)
        {
            CardPile pile = PileType.Hand.GetPile(player);
            List<CardModel> list2 = new List<CardModel>();
            List<CardModel> cardsToRetain = new List<CardModel>();
            foreach (CardModel card in pile.Cards)
            {
                if (card.ShouldRetainThisTurn)
                {
                    cardsToRetain.Add(card);
                }
                else
                {
                    list2.Add(card);
                }
            }
            if (Hook.ShouldFlush(player.Creature.CombatState, player))
            {
                CardPileCmd.Add(list2, PileType.Discard.GetPile(player));
            }
            foreach (CardModel item in cardsToRetain)
            {
                Hook.AfterCardRetained(_state, item);
            }
            player.PlayerCombatState.EndOfTurnCleanup();
        }
        Hook.AfterTurnEnd(_state, _state.CurrentSide);
        RunManager.Instance.ChecksumTracker.GenerateChecksum("after player turn phase two end", null);
    }

    public void SwitchFromPlayerToEnemySide(Action? actionDuringEnemyTurn = null)
    {
		if (_state == null)
		{
			return;
		}
        List<Player> list;
        lock (_playerReadyLock)
        {
            _playersTakingExtraTurn.Clear();
            foreach (Player player in _state.Players)
            {
                if (Hook.ShouldTakeExtraTurn(_state, player))
                {
                    Log.Info($"Player {player.NetId} ({player.Character.Id.Entry}) is taking an extra turn");
                    _playersTakingExtraTurn.Add(player);
                }
            }
            list = _playersTakingExtraTurn.ToList();
        }
        SwitchSides();
        foreach (Player item in list)
        {
			if (_state == null)
			{
				return;
			}
            Hook.AfterTakingExtraTurn(_state, item);
        }
        StartTurn(actionDuringEnemyTurn);
    }

    private void SwitchSides()
    {
		if (_state == null)
		{
			return;
		}
        bool flag;
        lock (_playerReadyLock)
        {
            flag = _playersTakingExtraTurn.Count > 0;
        }
        if (_state.CurrentSide == CombatSide.Player && !flag)
        {
            _state.CurrentSide = CombatSide.Enemy;
        }
        else
        {
            _state.CurrentSide = CombatSide.Player;
            _state.RoundNumber++;
        }
        foreach (Creature creature in _state.Creatures)
        {
            creature.OnSideSwitch();
        }
        this.TurnEnded?.Invoke(_state);
    }

	public bool IsPartOfPlayerTurn(Player player)
	{
		CombatState? state = _state;
		if (state == null || state.CurrentSide != CombatSide.Player)
		{
			return false;
		}
		if (_playersTakingExtraTurn.Count == 0)
		{
			return true;
		}
		return _playersTakingExtraTurn.Contains(player);
	}

    public void DebugForceTopCardOnNextShuffle(CardModel card)
    {
        card.AssertMutable();
        DebugForcedTopCardOnNextShuffle = card;
    }

    public void DebugClearForcedTopCardOnNextShuffle()
    {
        DebugForcedTopCardOnNextShuffle = null;
    }
}
