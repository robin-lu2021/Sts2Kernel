using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class PaelsEye : RelicModel
{
	private bool _usedThisCombat;

	private bool _anyCardsPlayedThisTurn;

	private bool _wasOwnerPartOfLastPlayerTurn = true;

	public override RelicRarity Rarity => RelicRarity.Ancient;

	private bool UsedThisCombat
	{
		get
		{
			return _usedThisCombat;
		}
		set
		{
			AssertMutable();
			_usedThisCombat = value;
		}
	}

	private bool AnyCardsPlayedThisTurn
	{
		get
		{
			return _anyCardsPlayedThisTurn;
		}
		set
		{
			AssertMutable();
			_anyCardsPlayedThisTurn = value;
		}
	}

	private bool WasOwnerPartOfLastPlayerTurn
	{
		get
		{
			return _wasOwnerPartOfLastPlayerTurn;
		}
		set
		{
			AssertMutable();
			_wasOwnerPartOfLastPlayerTurn = value;
		}
	}

	public override void AfterObtained()
	{
		WasOwnerPartOfLastPlayerTurn = CombatManager.Instance.IsPartOfPlayerTurn(base.Owner);
		return;
	}

	public override void BeforeCardPlayed(CardPlay cardPlay)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			return;
		}
		if (AnyCardsPlayedThisTurn || UsedThisCombat)
		{
			return;
		}
		if (cardPlay.Card.Owner != base.Owner)
		{
			return;
		}
		base.Status = RelicStatus.Normal;
		AnyCardsPlayedThisTurn = true;
		return;
	}

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		if (UsedThisCombat)
		{
			return;
		}
		base.Status = RelicStatus.Active;
		AnyCardsPlayedThisTurn = false;
		WasOwnerPartOfLastPlayerTurn = CombatManager.Instance.IsPartOfPlayerTurn(base.Owner);
		return;
	}

	public override bool ShouldTakeExtraTurn(Player player)
	{
		if (!UsedThisCombat && !AnyCardsPlayedThisTurn && WasOwnerPartOfLastPlayerTurn)
		{
			return player == base.Owner;
		}
		return false;
	}

	public override void BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (UsedThisCombat || AnyCardsPlayedThisTurn || !WasOwnerPartOfLastPlayerTurn || side != CombatSide.Player)
		{
			return;
		}
		foreach (CardModel item in CardPile.GetCards(base.Owner, PileType.Hand).ToList())
		{
			CardCmd.Exhaust(choiceContext, item);
		}
	}

	public override void AfterTakingExtraTurn(Player player)
	{
		if (player != base.Owner)
		{
			return;
		}
		 
		base.Status = RelicStatus.Normal;
		UsedThisCombat = true;
		return;
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		base.Status = RelicStatus.Normal;
		UsedThisCombat = false;
		return;
	}
}