using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class RoundTeaParty : EventModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DamageVar(11m, ValueProp.Unblockable | ValueProp.Unpowered),
		new StringVar("Relic", KernelModelDb.Relic<RoyalPoison>().Title.GetFormattedText())
	});

	public override bool IsAllowed(IRunState runState)
	{
		return runState.Players.All((Player p) => p.Creature.CurrentHp >= 12);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, EnjoyTea, "ROUND_TEA_PARTY.pages.INITIAL.options.ENJOY_TEA", KernelHoverTipFactory.FromRelic<RoyalPoison>()),
			new EventOption(this, PickFight, "ROUND_TEA_PARTY.pages.INITIAL.options.PICK_FIGHT").ThatDoesDamage(base.DynamicVars.Damage.BaseValue)
		});
	}

	private void EnjoyTea()
	{
		Creature targetCreature = base.Owner.Creature;
		RelicCmd.Obtain<RoyalPoison>(base.Owner);
		CreatureCmd.Heal(targetCreature, targetCreature.MaxHp - targetCreature.CurrentHp);
		SetEventFinished(L10NLookup("ROUND_TEA_PARTY.pages.ENJOY_TEA.description"));
	}

	private void PickFight()
	{
		SetEventState(L10NLookup("ROUND_TEA_PARTY.pages.PICK_FIGHT.description"), new global::_003C_003Ez__ReadOnlySingleElementList<EventOption>(new EventOption(this, ContinueFight, "ROUND_TEA_PARTY.pages.PICK_FIGHT.options.CONTINUE_FIGHT").ThatWontSaveToChoiceHistory()));
		return;
	}

	private void ContinueFight()
	{
		CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.DynamicVars.Damage, null, null);
		RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(base.Owner).ToMutable(), base.Owner);
		SetEventFinished(L10NLookup("ROUND_TEA_PARTY.pages.CONTINUE_FIGHT.description"));
	}
}

