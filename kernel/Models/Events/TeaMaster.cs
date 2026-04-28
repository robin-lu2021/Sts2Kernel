using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class TeaMaster : EventModel
{
	private const string _boneTeaCostKey = "BoneTeaCost";

	private const string _emberTeaCostKey = "EmberTeaCost";

	private const int _boneTeaCost = 50;

	private const int _emberTeaCost = 150;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[5]
	{
		new DynamicVar("BoneTeaCost", 50m),
		new DynamicVar("EmberTeaCost", 150m),
		new StringVar("BoneTeaDescription", KernelModelDb.Relic<BoneTea>().DynamicDescription.GetFormattedText()),
		new StringVar("EmberTeaDescription", KernelModelDb.Relic<EmberTea>().DynamicDescription.GetFormattedText()),
		new StringVar("TeaOfDiscourtesyDescription", KernelModelDb.Relic<TeaOfDiscourtesy>().DynamicDescription.GetFormattedText())
	});

	public override bool IsAllowed(IRunState runState)
	{
		if (runState.CurrentActIndex < 2)
		{
			return runState.Players.All((Player p) => p.Gold >= 150);
		}
		return false;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		List<EventOption> list = new List<EventOption>();
		if ((decimal)base.Owner.Gold >= base.DynamicVars["BoneTeaCost"].BaseValue)
		{
			list.Add(new EventOption(this, BoneTea, "TEA_MASTER.pages.INITIAL.options.BONE_TEA", KernelHoverTipFactory.FromRelicExcludingItself<BoneTea>()));
		}
		else
		{
			list.Add(new EventOption(this, null, "TEA_MASTER.pages.INITIAL.options.BONE_TEA_LOCKED"));
		}
		if ((decimal)base.Owner.Gold >= base.DynamicVars["EmberTeaCost"].BaseValue)
		{
			list.Add(new EventOption(this, EmberTea, "TEA_MASTER.pages.INITIAL.options.EMBER_TEA", KernelHoverTipFactory.FromRelicExcludingItself<EmberTea>()));
		}
		else
		{
			list.Add(new EventOption(this, null, "TEA_MASTER.pages.INITIAL.options.EMBER_TEA_LOCKED"));
		}
		list.Add(new EventOption(this, TeaOfDiscourtesy, "TEA_MASTER.pages.INITIAL.options.TEA_OF_DISCOURTESY", KernelHoverTipFactory.FromRelicExcludingItself<TeaOfDiscourtesy>()));
		return list;
	}

	private void BoneTea()
	{
		PlayerCmd.LoseGold(base.DynamicVars["BoneTeaCost"].BaseValue, base.Owner, GoldLossType.Spent);
		RelicCmd.Obtain<BoneTea>(base.Owner);
		SetEventFinished(L10NLookup("TEA_MASTER.pages.DONE.description"));
	}

	private void EmberTea()
	{
		PlayerCmd.LoseGold(base.DynamicVars["EmberTeaCost"].BaseValue, base.Owner, GoldLossType.Spent);
		RelicCmd.Obtain<EmberTea>(base.Owner);
		SetEventFinished(L10NLookup("TEA_MASTER.pages.DONE.description"));
	}

	private void TeaOfDiscourtesy()
	{
		RelicCmd.Obtain<TeaOfDiscourtesy>(base.Owner);
		SetEventFinished(L10NLookup("TEA_MASTER.pages.TEA_OF_DISCOURTESY.description"));
	}
}

