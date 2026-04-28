using System;
using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class PaelsTooth : RelicModel
{
	public const int cardsCount = 5;

	private const string _cardTitlesKey = "CardTitles";

	private List<SerializableCard> _serializableCards = new List<SerializableCard>();

	public override RelicRarity Rarity => RelicRarity.Ancient;



	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new CardsVar(5),
		new StringVar("CardTitles")
	});

	[SavedProperty]
	public List<SerializableCard> SerializableCards
	{
		get
		{
			return _serializableCards;
		}
		private set
		{
			AssertMutable();
			_serializableCards.Clear();
			_serializableCards.AddRange(value);
			UpdateCardList();
		}
	}

	protected override void AfterCloned()
	{
		base.AfterCloned();
		_serializableCards = new List<SerializableCard>();
	}

	public override void AfterObtained()
	{
		IEnumerable<CardModel> enumerable = (CardSelectCmd.FromDeckForRemoval(prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, base.DynamicVars.Cards.IntValue), player: base.Owner, filter: (CardModel c) => c.IsUpgradable)).OrderBy<CardModel, string>((CardModel c) => c.Id.Entry, StringComparer.Ordinal);
		foreach (CardModel item in enumerable)
		{
			CardModel cardModel = (CardModel)item.MutableClone();
			SerializableCards.Add(cardModel.ToSerializable());
			CardPileCmd.RemoveFromDeck(item);
		}
		UpdateCardList();
	}

	public override void AfterCombatEnd(CombatRoom room)
	{
		if (!base.Owner.Creature.IsDead && SerializableCards.Count != 0)
		{
			 
			SerializableCard serializableCard = base.Owner.PlayerRng.Rewards.NextItem(SerializableCards);
			CardModel cardModel = CardModel.FromSerializable(serializableCard);
			if (!base.Owner.RunState.ContainsCard(cardModel))
			{
				base.Owner.RunState.AddCard(cardModel, base.Owner);
			}
			if (cardModel.IsUpgradable)
			{
				CardCmd.Upgrade(cardModel, CardPreviewStyle.MessyLayout);
			}
			CardCmd.PreviewCardPileAdd(CardPileCmd.Add(cardModel, PileType.Deck));
			base.Status = ((SerializableCards.Count <= 0) ? RelicStatus.Disabled : RelicStatus.Normal);
			SerializableCards.Remove(serializableCard);
			UpdateCardList();
		}
	}

	private void UpdateCardList()
	{
		base.Status = ((SerializableCards.Count <= 0) ? RelicStatus.Disabled : RelicStatus.Normal);
		StringVar stringVar = (StringVar)base.DynamicVars["CardTitles"];
		if (SerializableCards.Count == 0)
		{
			stringVar.StringValue = string.Empty;
		}
		else
		{
			stringVar.StringValue = string.Join('\n', SerializableCards.Select((SerializableCard c) => "- " + SaveUtil.CardOrDeprecated(c.Id).Title));
		}
		InvokeDisplayAmountChanged();
	}

	public void DebugAddCard(SerializableCard card)
	{
		SerializableCards.Add(card);
	}
}