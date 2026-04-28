using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class VelvetChoker : RelicModel
{
	private int _cardsPlayedThisTurn;

	public override RelicRarity Rarity => RelicRarity.Ancient;



	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new CardsVar(6),
		new EnergyVar(1)
	});


	private bool ShouldPreventCardPlay => _cardsPlayedThisTurn >= base.DynamicVars.Cards.IntValue;

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != base.Owner)
		{
			return amount;
		}
		return amount + base.DynamicVars.Energy.BaseValue;
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType _)
	{
		if (card.Owner != base.Owner)
		{
			return true;
		}
		return !ShouldPreventCardPlay;
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == base.Owner)
		{
			_cardsPlayedThisTurn++;
		}
		InvokeDisplayAmountChanged();
		return;
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (!(room is CombatRoom))
		{
			return;
		}
		_cardsPlayedThisTurn = 0;
		InvokeDisplayAmountChanged();
		return;
	}

	public override void AfterCombatEnd(CombatRoom room)
	{
		_cardsPlayedThisTurn = 0;
		InvokeDisplayAmountChanged();
		return;
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		_cardsPlayedThisTurn = 0;
		InvokeDisplayAmountChanged();
		return;
	}
}