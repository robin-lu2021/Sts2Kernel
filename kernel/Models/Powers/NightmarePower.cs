using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class NightmarePower : PowerModel
{
	private class Data
	{
		public CardModel? selectedCard;
	}

	private const string _cardKey = "Card";

	public override PowerType Type => PowerType.Buff;

	public override bool IsInstanced => true;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new StringVar("Card"));

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override void BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
	{
		if (player == base.Owner.Player)
		{
			CardModel card = GetInternalData<Data>().selectedCard;
			for (int i = 0; i < base.Amount; i++)
			{
				CardModel card2 = card.CreateClone();
				CardPileCmd.AddGeneratedCardToCombat(card2, PileType.Hand, addedByPlayer: true);
			}
			PowerCmd.Remove(this);
		}
	}

	public void SetSelectedCard(CardModel card)
	{
		GetInternalData<Data>().selectedCard = card.CreateClone();
		((StringVar)base.DynamicVars["Card"]).StringValue = card.Title;
	}
}
