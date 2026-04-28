using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class TuningFork : RelicModel
{
	private bool _isActivating;

	private int _skillsPlayed;

	public override RelicRarity Rarity => RelicRarity.Uncommon;




	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new CardsVar(10),
		new BlockVar(7m, ValueProp.Unpowered)
	});

	private bool IsActivating
	{
		get
		{
			return _isActivating;
		}
		set
		{
			AssertMutable();
			_isActivating = value;
			UpdateDisplay();
		}
	}

	[SavedProperty]
	public int SkillsPlayed
	{
		get
		{
			return _skillsPlayed;
		}
		private set
		{
			AssertMutable();
			if (_skillsPlayed != value)
			{
				_skillsPlayed = value;
				UpdateDisplay();
			}
		}
	}

	private int SkillsThreshold => base.DynamicVars.Cards.IntValue;

	private void UpdateDisplay()
	{
		if (IsActivating)
		{
			base.Status = RelicStatus.Normal;
		}
		else
		{
			base.Status = ((SkillsPlayed == SkillsThreshold - 1) ? RelicStatus.Active : RelicStatus.Normal);
		}
		InvokeDisplayAmountChanged();
	}

	public void NotifySkillPlayed()
	{
		SkillsPlayed++;
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == base.Owner && cardPlay.Card.Type == CardType.Skill)
		{
			SkillsPlayed++;
			if (SkillsPlayed >= SkillsThreshold)
			{
				DoActivateVisuals();
				CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, null);
				SkillsPlayed -= SkillsThreshold;
			}
		}
	}

	private void DoActivateVisuals()
	{
		IsActivating = true;
		 
		IsActivating = false;
	}
}
