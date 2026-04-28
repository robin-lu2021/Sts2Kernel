using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class StoneOfAllTime : EventModel
{
	private const string _drinkRandomPotionKey = "DrinkRandomPotion";

	private const string _drinkMaxHpGain = "DrinkMaxHpGain";

	private const string _pushHpLoss = "PushHpLoss";

	private const string _pushVigorousAmountKey = "PushVigorousAmount";

	private PotionModel? _drinkAndLiftPotion;

	private PotionModel? DrinkAndLiftPotion
	{
		get
		{
			return _drinkAndLiftPotion;
		}
		set
		{
			AssertMutable();
			_drinkAndLiftPotion = value;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[4]
	{
		new StringVar("DrinkRandomPotion"),
		new DynamicVar("DrinkMaxHpGain", 10m),
		new DynamicVar("PushHpLoss", 6m),
		new DynamicVar("PushVigorousAmount", 8m)
	});

	public override bool IsAllowed(IRunState runState)
	{
		if (runState.CurrentActIndex == 1)
		{
			return runState.Players.All((Player player) => player.Potions.Any());
		}
		return false;
	}

	protected override void BeforeEventStarted(bool isPreFinished)
	{
		base.Owner.CanRemovePotions = false;
		return;
	}

	protected override void OnEventFinished()
	{
		base.Owner.CanRemovePotions = true;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		DrinkAndLiftPotion = base.Rng.NextItem(base.Owner.Potions);
		EventOption eventOption;
		if (DrinkAndLiftPotion != null)
		{
			StringVar stringVar = (StringVar)base.DynamicVars["DrinkRandomPotion"];
			stringVar.StringValue = DrinkAndLiftPotion.Title.GetFormattedText();
			eventOption = new EventOption(this, Lift, "STONE_OF_ALL_TIME.pages.INITIAL.options.LIFT", KernelHoverTipFactory.FromPotion(DrinkAndLiftPotion));
		}
		else
		{
			eventOption = new EventOption(this, null, "STONE_OF_ALL_TIME.pages.INITIAL.options.LIFT_LOCKED");
		}
		bool canPush = CardPile.Get(PileType.Deck, base.Owner).Cards.Count((CardModel c) => ModelDb.Enchantment<Vigorous>().CanEnchant(c)) >= 1;
		EventOption eventOption2 = (!canPush) ? new EventOption(this, null, "STONE_OF_ALL_TIME.pages.INITIAL.options.PUSH_LOCKED") : new EventOption(this, Push, "STONE_OF_ALL_TIME.pages.INITIAL.options.PUSH", HoverTipFactory.FromEnchantment<Vigorous>(base.DynamicVars["PushVigorousAmount"].IntValue).ThatDoesDamage(base.DynamicVars["PushHpLoss"].BaseValue));
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2] { eventOption, eventOption2 });
	}

	private void Lift()
	{
		PotionCmd.Discard(DrinkAndLiftPotion);
		CreatureCmd.GainMaxHp(base.Owner.Creature, base.DynamicVars["DrinkMaxHpGain"].BaseValue);
		base.Rng.NextInt(100);
		LocString locString = L10NLookup("STONE_OF_ALL_TIME.pages.LIFT.description");
		locString.Add(base.DynamicVars["DrinkRandomPotion"]);
		SetEventFinished(locString);
	}

	private void Push()
	{
		CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.DynamicVars["PushHpLoss"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
		Vigorous vigorous = ModelDb.Enchantment<Vigorous>();
		foreach (CardModel item in CardSelectCmd.FromDeckForEnchantment(base.Owner, vigorous, base.DynamicVars["PushVigorousAmount"].IntValue, prefs))
		{
			CardCmd.Enchant(vigorous.ToMutable(), item, base.DynamicVars["PushVigorousAmount"].BaseValue);
		}
		base.Rng.NextInt(100);
		SetEventFinished(L10NLookup("STONE_OF_ALL_TIME.pages.PUSH.description"));
	}
}
