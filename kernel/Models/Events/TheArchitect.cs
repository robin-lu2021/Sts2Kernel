using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Models.Events;

/*
Kernel version of TheArchitect.

The original implementation used a combat-room staging setup, speech bubbles,
animation tracks, VFX, and timed waits to sell the encounter. For the headless
kernel we keep only the deterministic dialogue selection and line-by-line event
progression:
1. choose the same dialogue set based on character / visit history
2. expose one continue or respond option per line
3. advance synchronously through the conversation
4. mark the local player ready for the act transition when the dialogue ends

Everything presentation-facing is intentionally removed.
*/

public sealed class TheArchitect : EventModel
{
	private static readonly LocString _emptyLocString = new LocString("ancients", "PROCEED.description");

	private static readonly LocString _continueLocString = new LocString("ancients", "THE_ARCHITECT.CONTINUE");

	private static readonly LocString _respondLocString = new LocString("ancients", "THE_ARCHITECT.RESPOND");

	private AncientDialogue? _dialogue;

	private int _currentLineIndex;

	private AncientDialogueSet? _dialogueSet;

	public override EncounterModel CanonicalEncounter => ModelDb.Encounter<TheArchitectEventEncounter>();

	public override string LocTable => "ancients";

	public override IEnumerable<LocString> GameInfoOptions => Array.Empty<LocString>();

	private AncientDialogue? Dialogue
	{
		get
		{
			return _dialogue;
		}
		set
		{
			AssertMutable();
			_dialogue = value;
		}
	}

	private int CurrentLineIndex
	{
		get
		{
			return _currentLineIndex;
		}
		set
		{
			AssertMutable();
			_currentLineIndex = value;
		}
	}

	public AncientDialogueSet DialogueSet
	{
		get
		{
			if (_dialogueSet == null)
			{
				_dialogueSet = DefineDialogues();
				_dialogueSet.PopulateLocKeys(base.Id);
			}
			return _dialogueSet;
		}
	}

	protected override void SetInitialEventState(bool isPreFinished)
	{
		if (isPreFinished)
		{
			SetEventFinished(_emptyLocString);
			return;
		}
		IReadOnlyList<myEventOption> options = GenerateInitialOptionsWrapper();
		SetEventState(GetCurrentDescription(), options);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		LoadDialogue();
		if (Dialogue == null || Dialogue.Lines.Count == 0)
		{
			return new global::_003C_003Ez__ReadOnlySingleElementList<EventOption>(CreateProceedOption());
		}
		CurrentLineIndex = 0;
		return new global::_003C_003Ez__ReadOnlySingleElementList<EventOption>(CreateOptionForCurrentLine());
	}

	private LocString GetCurrentDescription()
	{
		if (Dialogue == null || CurrentLineIndex >= Dialogue.Lines.Count)
		{
			return _emptyLocString;
		}
		return Dialogue.Lines[CurrentLineIndex].LineText ?? _emptyLocString;
	}

	private EventOption CreateOptionForCurrentLine()
	{
		if (Dialogue == null || CurrentLineIndex >= Dialogue.Lines.Count)
		{
			return CreateProceedOption();
		}
		AncientDialogueLine currentLine = Dialogue.Lines[CurrentLineIndex];
		if (CurrentLineIndex >= Dialogue.Lines.Count - 1)
		{
			return CreateProceedOption();
		}
		LocString title = currentLine.NextButtonText ?? ((currentLine.Speaker != AncientDialogueSpeaker.Ancient) ? _continueLocString : _respondLocString);
		return (EventOption)new EventOption(this, AdvanceDialogue, title, _emptyLocString, $"{base.Id}.dialogue.{CurrentLineIndex}", Array.Empty<IHoverTip>()).ThatWontSaveToChoiceHistory();
	}

	private EventOption CreateProceedOption()
	{
		return new EventOption(this, WinRun, "PROCEED", false, false).ThatWontSaveToChoiceHistory();
	}

	private void AdvanceDialogue()
	{
		CurrentLineIndex++;
		if (Dialogue == null || CurrentLineIndex >= Dialogue.Lines.Count)
		{
			SetEventState(_emptyLocString, new global::_003C_003Ez__ReadOnlySingleElementList<EventOption>(CreateProceedOption()));
			return;
		}
		SetEventState(GetCurrentDescription(), new global::_003C_003Ez__ReadOnlySingleElementList<EventOption>(CreateOptionForCurrentLine()));
	}

	private void WinRun()
	{
		RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();
		SetEventFinished(_emptyLocString);
	}

	private void LoadDialogue()
	{
		int charVisits = SaveManager.Instance.Progress.GetStatsForCharacter(base.Owner.Character.Id)?.TotalWins ?? 0;
		int wins = SaveManager.Instance.Progress.Wins;
		List<AncientDialogue> validDialogues = DialogueSet.GetValidDialogues(base.Owner.Character.Id, charVisits, wins, allowAnyCharacterDialogues: false).ToList();
		Dialogue = ((validDialogues.Count > 0) ? base.Rng.NextItem(validDialogues) : null);
	}

	private static string CharKey<T>() where T : CharacterModel
	{
		return ModelDb.Character<T>().Id.Entry;
	}

	private static AncientDialogueSet DefineDialogues()
	{
		return new AncientDialogueSet
		{
			FirstVisitEverDialogue = null,
			CharacterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>
			{
				[CharKey<Ironclad>()] = new global::_003C_003Ez__ReadOnlyArray<AncientDialogue>(new AncientDialogue[3]
				{
					new AncientDialogue("", "")
					{
						VisitIndex = 0,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("", "", "")
					{
						VisitIndex = 1,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("", "", "")
					{
						VisitIndex = 2,
						EndAttackers = ArchitectAttackers.Both
					}
				}),
				[CharKey<Silent>()] = new global::_003C_003Ez__ReadOnlyArray<AncientDialogue>(new AncientDialogue[4]
				{
					new AncientDialogue("")
					{
						VisitIndex = 0,
						StartAttackers = ArchitectAttackers.Player,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("")
					{
						VisitIndex = 1,
						StartAttackers = ArchitectAttackers.Player,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("")
					{
						VisitIndex = 2,
						StartAttackers = ArchitectAttackers.Player,
						EndAttackers = ArchitectAttackers.Architect
					},
					new AncientDialogue("")
					{
						VisitIndex = 3,
						StartAttackers = ArchitectAttackers.Player,
						EndAttackers = ArchitectAttackers.Architect
					}
				}),
				[CharKey<Defect>()] = new global::_003C_003Ez__ReadOnlyArray<AncientDialogue>(new AncientDialogue[3]
				{
					new AncientDialogue("", "", "")
					{
						VisitIndex = 0,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("", "", "")
					{
						VisitIndex = 1,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("", "", "")
					{
						VisitIndex = 2,
						EndAttackers = ArchitectAttackers.Both
					}
				}),
				[CharKey<Necrobinder>()] = new global::_003C_003Ez__ReadOnlyArray<AncientDialogue>(new AncientDialogue[4]
				{
					new AncientDialogue("", "")
					{
						VisitIndex = 0,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("", "")
					{
						VisitIndex = 1,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("", "")
					{
						VisitIndex = 2,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("", "", "")
					{
						VisitIndex = 3,
						EndAttackers = ArchitectAttackers.Both
					}
				}),
				[CharKey<Regent>()] = new global::_003C_003Ez__ReadOnlyArray<AncientDialogue>(new AncientDialogue[3]
				{
					new AncientDialogue("", "", "")
					{
						VisitIndex = 0,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("", "", "")
					{
						VisitIndex = 1,
						EndAttackers = ArchitectAttackers.Both
					},
					new AncientDialogue("", "", "")
					{
						VisitIndex = 2,
						EndAttackers = ArchitectAttackers.Both
					}
				})
			},
			AgnosticDialogues = Array.Empty<AncientDialogue>()
		};
	}
}
