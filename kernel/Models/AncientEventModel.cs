using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace MegaCrit.Sts2.Core;

/*
This is the no-GUI ancient-event companion to EventModel.

Ancients still need a bit more than regular events even in kernel mode:
1. dialogue-set definition and loc-key population
2. ancient-specific pre-finished flow
3. healed-on-visit behavior
4. relic-granting helper options
5. ancient choice history capture

Everything here stays synchronous and intentionally drops presentation-only
concerns such as colors, ambient BGM, map node textures, background scenes,
and room layout selection.
*/

public abstract class AncientEventModel : EventModel
{
	private AncientDialogueSet? _dialogueSet;

	private List<myEventOption>? _generatedOptions;

	protected string? _customDonePage;

	private string? _debugOption;

	public override string LocTable => "ancients";

	public LocString Epithet => L10NLookup(ModelId.Entry + ".epithet");

	public AncientDialogueSet DialogueSet
	{
		get
		{
			if (_dialogueSet == null)
			{
				_dialogueSet = DefineDialogues();
				_dialogueSet.PopulateLocKeys(Id);
			}
			return _dialogueSet;
		}
	}

	public virtual IEnumerable<CharacterModel> AnyCharacterDialogueBlacklist => Array.Empty<CharacterModel>();

	public int HealedAmount { get; private set; }

	public string? DebugOption
	{
		get => _debugOption;
		set => _debugOption = value;
	}

	public abstract IEnumerable<myEventOption> AllPossibleOptions { get; }

	public override LocString InitialDescription => IsAncientAllowed() ? base.InitialDescription : new LocString("relics", "WAX_CHOKER.blockMessage");

	protected abstract AncientDialogueSet DefineDialogues();

	protected static string CharKey<T>() where T : CharacterModel
	{
		return ModelDb.Character<T>().Id.Entry;
	}

	protected override void BeforeEventStarted(bool isPreFinished)
	{
		if (!isPreFinished)
		{
			if (this is Neow)
			{
				base.Owner.Creature.SetCurrentHpInternal(0m);
			}
			int oldHp = base.Owner.Creature.CurrentHp;
			decimal amount = base.Owner.Creature.MaxHp - base.Owner.Creature.CurrentHp;
			if (RunManager.Instance.HasAscension(AscensionLevel.WearyTraveler))
			{
				amount *= 0.8m;
			}
			CreatureCmd.Heal(base.Owner.Creature, amount, playAnim: false);
			HealedAmount = base.Owner.Creature.CurrentHp - oldHp;
		}
	}

	protected sealed override IReadOnlyList<myEventOption> GenerateInitialOptionsWrapper()
	{
		if (!IsAncientAllowed())
		{
			return new[] { new myEventOption(this, (Action?)null, "PROCEED", disableOnChosen: false, isProceed: true) };
		}
		_generatedOptions = GenerateInitialOptions().ToList();
		if (!string.IsNullOrWhiteSpace(DebugOption) && _generatedOptions.Count > 0)
		{
			myEventOption? debugOption = AllPossibleOptions.FirstOrDefault((myEventOption option) => option.TextKey.Contains(DebugOption, StringComparison.OrdinalIgnoreCase));
			if (debugOption != null)
			{
				_generatedOptions.RemoveAt(0);
				_generatedOptions.Insert(0, debugOption);
			}
		}
		ReplaceNullOptions(_generatedOptions);
		return _generatedOptions;
	}

	protected override void SetInitialEventState(bool isPreFinished)
	{
		IReadOnlyList<myEventOption> options = GenerateInitialOptionsWrapper();
		if (options.Count == 0 || isPreFinished)
		{
			StartPreFinished();
		}
		else
		{
			SetEventState(InitialDescription, options);
		}
	}

	public void StartPreFinished()
	{
		SetEventFinished(L10NLookup(_customDonePage ?? (Id + ".pages.DONE.description")));
	}

	protected void Done()
	{
		UpdateRunHistory();
		SetEventFinished(L10NLookup(_customDonePage ?? (Id + ".pages.DONE.description")));
	}

	protected myEventOption RelicOption<TRelic>(string pageName = "INITIAL", string? customDonePage = null) where TRelic : RelicModel
	{
		RelicModel mutableRelic = ModelDb.Relic<TRelic>().ToMutable();
		return RelicOption(mutableRelic, OnChosen, pageName);

		void OnChosen()
		{
			if (Owner == null)
			{
				throw new InvalidOperationException($"Ancient '{Id}' does not have an owner.");
			}
			RelicCmd.Obtain(mutableRelic, Owner);
			_customDonePage = customDonePage;
			Done();
		}
	}

	private void UpdateRunHistory()
	{
		if (Owner?.RunState.CurrentMapPointHistoryEntry == null || _generatedOptions == null)
		{
			return;
		}
		foreach (myEventOption option in _generatedOptions)
		{
			AncientChoiceHistoryEntry item = new AncientChoiceHistoryEntry(option.Title, option.WasChosen);
			Owner.RunState.CurrentMapPointHistoryEntry.GetEntry(Owner.NetId).AncientChoices.Add(item);
		}
	}

	private bool IsAncientAllowed()
	{
		if (Owner == null)
		{
			return true;
		}
		if (this is not AncientEventModel ancient)
		{
			return true;
		}
		return Hook.ShouldAllowAncient(Owner.RunState, Owner, ancient);
	}
}
