using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class TabletOfTruth : EventModel
{
	private const string _smashHpGainKey = "SmashHPGain";

	private const string _decipherHpLossKey = "DecipherMaxHpLoss";

	private int _decipherCount;

	private int DecipherCount
	{
		get
		{
			return _decipherCount;
		}
		set
		{
			AssertMutable();
			_decipherCount = value;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DynamicVar("SmashHPGain", 20m),
		new DynamicVar("DecipherMaxHpLoss", 3m)
	});

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, Decipher, "TABLET_OF_TRUTH.pages.INITIAL.options.DECIPHER_1").ThatWillKillPlayerIf((Player p) => (decimal)p.Creature.MaxHp <= base.DynamicVars["DecipherMaxHpLoss"].BaseValue),
			new EventOption(this, Smash, "TABLET_OF_TRUTH.pages.INITIAL.options.SMASH")
		});
	}

	private void Smash()
	{
		Creature creature = base.Owner.Creature;
		CreatureCmd.Heal(creature, base.DynamicVars["SmashHPGain"].BaseValue);
		SetEventFinished(L10NLookup("TABLET_OF_TRUTH.pages.SMASH.description"));
	}

	private void Decipher()
	{
		LoseMaxHpAndUpgrade(base.DynamicVars["DecipherMaxHpLoss"].BaseValue);
		DecipherCount++;
		if (DecipherCount == 5)
		{
			SetEventFinished(L10NLookup($"TABLET_OF_TRUTH.pages.DECIPHER_{DecipherCount}.description"));
			return;
		}
		base.DynamicVars["DecipherMaxHpLoss"].BaseValue = GetDecipherCost();
		SetEventState(L10NLookup($"TABLET_OF_TRUTH.pages.DECIPHER_{DecipherCount}.description"), new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, Decipher, $"TABLET_OF_TRUTH.pages.DECIPHER_{DecipherCount}.options.DECIPHER").ThatWillKillPlayerIf((Player p) => (decimal)p.Creature.MaxHp <= base.DynamicVars["DecipherMaxHpLoss"].BaseValue),
			new EventOption(this, GiveUp, "TABLET_OF_TRUTH.pages.DECIPHER.options.GIVE_UP")
		}));
	}

	public int GetDecipherCost()
	{
		Player owner = base.Owner;
		switch (DecipherCount)
		{
		case 1:
			return 6;
		case 2:
			return 12;
		case 3:
			return 24;
		case 4:
			return owner.Creature.MaxHp - 1;
		default:
			Log.Error($"DecipherCount: {DecipherCount} should not be called.");
			return 999999999;
		}
	}

	private void GiveUp()
	{
		SetEventFinished(L10NLookup("TABLET_OF_TRUTH.pages.GIVE_UP.description"));
	}

	private void LoseMaxHpAndUpgrade(decimal hp)
	{
		if (!(hp < (decimal)base.Owner.Creature.MaxHp))
		{
			CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.Owner.Creature.MaxHp - 1, isFromCard: false);
			CreatureCmd.Kill(base.Owner.Creature);
			return;
		}
		CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), base.Owner.Creature, hp, isFromCard: false);
		List<CardModel> list = PileType.Deck.GetPile(base.Owner).Cards.Where((CardModel c) => c.IsUpgradable).ToList();
		if (_decipherCount == 4)
		{
			foreach (CardModel item in list)
			{
				CardCmd.Upgrade(item, CardPreviewStyle.None);
			}
		}
		else if (list.Count != 0)
		{
			CardModel card = base.Rng.NextItem(list);
			CardCmd.Upgrade(card, CardPreviewStyle.None);
		}
	}
}
