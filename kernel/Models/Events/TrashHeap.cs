using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class TrashHeap : EventModel
{
	private static RelicModel[] Relics => new RelicModel[5]
	{
		KernelModelDb.Relic<DarkstonePeriapt>(),
		KernelModelDb.Relic<DreamCatcher>(),
		KernelModelDb.Relic<HandDrill>(),
		KernelModelDb.Relic<MawBank>(),
		KernelModelDb.Relic<TheBoot>()
	};

	private static CardModel[] Cards => new CardModel[10]
	{
		KernelModelDb.Card<Caltrops>(),
		KernelModelDb.Card<Clash>(),
		KernelModelDb.Card<Distraction>(),
		KernelModelDb.Card<DualWield>(),
		KernelModelDb.Card<Entrench>(),
		KernelModelDb.Card<HelloWorld>(),
		KernelModelDb.Card<Outmaneuver>(),
		KernelModelDb.Card<Rebound>(),
		KernelModelDb.Card<RipAndTear>(),
		KernelModelDb.Card<Stack>()
	};

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new HpLossVar(8m),
		new GoldVar(100)
	});

	public override bool IsAllowed(IRunState runState)
	{
		return runState.Players.All((Player player) => player.Creature.CurrentHp > 5);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, DiveIn, "TRASH_HEAP.pages.INITIAL.options.DIVE_IN").ThatDoesDamage(base.DynamicVars.HpLoss.IntValue),
			new EventOption(this, Grab, "TRASH_HEAP.pages.INITIAL.options.GRAB")
		});
	}

	private void DiveIn()
	{
		CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.DynamicVars.HpLoss.IntValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
		RelicModel relicModel = base.Rng.NextItem(Relics);
		RelicCmd.Obtain(relicModel.ToMutable(), base.Owner);
		SetEventFinished(L10NLookup("TRASH_HEAP.pages.DIVE_IN.description"));
	}

	private void Grab()
	{
		PlayerCmd.GainGold(base.DynamicVars.Gold.BaseValue, base.Owner);
		CardModel card = base.Owner.RunState.CreateCard(base.Rng.NextItem(Cards), base.Owner);
		CardPileCmd.Add(card, PileType.Deck);
		SetEventFinished(L10NLookup("TRASH_HEAP.pages.GRAB.description"));
	}
}

