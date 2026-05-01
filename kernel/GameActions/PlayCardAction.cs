using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core.GameActions;

public sealed class PlayCardAction : GameAction
{
	private CardModel? _card;

	public override ulong OwnerId => Player.NetId;

	public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

	public Player Player { get; }

	public NetCombatCard NetCombatCard { get; }

	public ModelId CardModelId { get; }

	public uint? TargetId { get; }

	public PlayerChoiceContext? PlayerChoiceContext { get; private set; }

	public Creature? Target => Player.Creature.CombatState?.GetCreature(TargetId);

	public PlayCardAction(CardModel cardModel, Creature? target)
	{
		target = NormalizeCardTarget(cardModel, target);
		if (target != null && !target.CombatId.HasValue)
		{
			throw new InvalidOperationException($"Cannot target card against target {target} with no combat ID!");
		}
		Player = cardModel.Owner;
		NetCombatCard = NetCombatCard.FromModel(cardModel);
		CardModelId = cardModel.Id;
		TargetId = target?.CombatId;
	}

	public PlayCardAction(Player player, NetCombatCard netCombatCard, ModelId cardModelId, uint? targetId)
	{
		Player = player;
		NetCombatCard = netCombatCard;
		CardModelId = cardModelId;
		TargetId = targetId;
	}

	public override Task ExecuteAction()
	{
		_card = NetCombatCard.ToCardModel();
		Creature? target = NormalizeCardTarget(_card, Player.Creature.CombatState.GetCreature(TargetId));
		CardPile? pile = _card.Pile;
		if (pile == null || pile.Type != PileType.Hand)
		{
			return Task.CompletedTask;
		}
		bool flag = target == null;
		bool flag2 = flag;
		if (flag2)
		{
			TargetType targetType = _card.TargetType;
			bool flag3 = ((targetType == TargetType.AnyEnemy || targetType == TargetType.AnyAlly) ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			Log.Warn($"Attempted to play card {_card} with TargetType of type 'Any', but no target was passed to the play card action!");
		}
		if (!_card.CanPlay(out UnplayableReason _, out AbstractModel _) || !_card.IsValidTarget(target))
		{
			Cancel();
			return Task.CompletedTask;
		}
		CardPileCmd.AddDuringManualCardPlay(_card);
		string value = ((target != null) ? $"targeting {target.LogName} (index {Player.Creature.CombatState?.Creatures.IndexOf(target)})" : "no target");
		Log.Info($"Player {_card.Owner.NetId} playing card {_card.Id.Entry} ({value})");
		int item = _card.GetEnergyCostToPay();
		int item2 = _card.GetStarCostToPay();
		_card.SpendResources();
		ResourceInfo resources = new ResourceInfo
		{
			EnergySpent = item,
			EnergyValue = item,
			StarsSpent = item2,
			StarValue = item2
		};
		PlayerChoiceContext = new GameActionPlayerChoiceContext(this);
		_card.OnPlayWrapper(PlayerChoiceContext, target, isAutoPlay: false, resources);
		return Task.CompletedTask;
	}

	private static Creature? NormalizeCardTarget(CardModel cardModel, Creature? target)
	{
		return cardModel.TargetType == TargetType.AnyEnemy || cardModel.TargetType == TargetType.AnyAlly ? target : null;
	}

	protected override void CancelAction()
	{
		if (_card == null)
		{
			_card = NetCombatCard.ToCardModelOrNull();
		}
	}

	public override INetAction ToNetAction()
	{
		NetPlayCardAction netPlayCardAction = new NetPlayCardAction
		{
			card = NetCombatCard,
			modelId = CardModelId,
			targetId = TargetId
		};
		return netPlayCardAction;
	}

	public override string ToString()
	{
		CardModel value = NetCombatCard.ToCardModelOrNull();
		return $"{"PlayCardAction"} card: {value} index: {NetCombatCard.CombatCardIndex} targetid: {TargetId}";
	}
}
