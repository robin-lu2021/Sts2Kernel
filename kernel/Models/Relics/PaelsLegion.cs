using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class PaelsLegion : RelicModel
{
	private const string _turnsKey = "Turns";

	private string _skin = SkinOptions[0];

	private int _cooldown;

	private bool _triggeredBlockLastTurn;

	private CardPlay? _affectedCardPlay;

	public override bool AddsPet => true;

	public override RelicRarity Rarity => RelicRarity.Ancient;

	public static string[] SkinOptions => new string[4] { "eyes", "horns", "spikes", "wings" };

	[SavedProperty]
	public string Skin
	{
		get
		{
			return _skin;
		}
		set
		{
			AssertMutable();
			_skin = value;
		}
	}


	private int Cooldown
	{
		get
		{
			return _cooldown;
		}
		set
		{
			AssertMutable();
			_cooldown = value;
			InvokeDisplayAmountChanged();
		}
	}

	private bool TriggeredBlockLastTurn
	{
		get
		{
			return _triggeredBlockLastTurn;
		}
		set
		{
			AssertMutable();
			_triggeredBlockLastTurn = value;
			InvokeDisplayAmountChanged();
		}
	}

	private CardPlay? AffectedCardPlay
	{
		get
		{
			return _affectedCardPlay;
		}
		set
		{
			AssertMutable();
			_affectedCardPlay = value;
			InvokeDisplayAmountChanged();
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("Turns", 2m));

	public override void AfterObtained()
	{
		Skin = base.Owner.RunState.Rng.Niche.NextItem(SkinOptions) ?? SkinOptions[0];
		if (CombatManager.Instance.IsInProgress)
		{
			SummonPet();
		}
	}

	public override void BeforeCombatStart()
	{
		SummonPet();
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		if (!props.IsCardOrMonsterMove())
		{
			return 1m;
		}
		if (cardSource == null)
		{
			return 1m;
		}
		if (target != base.Owner.Creature)
		{
			return 1m;
		}
		if (Cooldown > 0)
		{
			return 1m;
		}
		return 2m;
	}

	public override void AfterModifyingBlockAmount(decimal modifiedAmount, CardModel? cardSource, CardPlay? cardPlay)
	{
		if (modifiedAmount <= 0m)
		{
			return;
		}
		if (cardPlay == null)
		{
			return;
		}
		if (AffectedCardPlay != null && AffectedCardPlay != cardPlay)
		{
			return;
		}
		AffectedCardPlay = cardPlay;
		return;
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (AffectedCardPlay != null && AffectedCardPlay == cardPlay)
		{
			 
			AffectedCardPlay = null;
			Cooldown = base.DynamicVars["Turns"].IntValue;
			base.Status = RelicStatus.Normal;
			MegaCrit.Sts2.Core.Models.Monsters.PaelsLegion paelsLegion = (MegaCrit.Sts2.Core.Models.Monsters.PaelsLegion)base.Owner.PlayerCombatState.GetPet<MegaCrit.Sts2.Core.Models.Monsters.PaelsLegion>().Monster;
			TriggeredBlockLastTurn = true;
		}
	}

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side == base.Owner.Creature.Side)
		{
			bool flag = Cooldown > 0;
			Cooldown--;
			if (Cooldown <= 0)
			{
				base.Status = RelicStatus.Active;
				InvokeDisplayAmountChanged();
			}
			MegaCrit.Sts2.Core.Models.Monsters.PaelsLegion paelsLegion = (MegaCrit.Sts2.Core.Models.Monsters.PaelsLegion)base.Owner.PlayerCombatState.GetPet<MegaCrit.Sts2.Core.Models.Monsters.PaelsLegion>().Monster;
			TriggeredBlockLastTurn = false;
		}
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		base.Status = RelicStatus.Normal;
		Cooldown = 0;
		TriggeredBlockLastTurn = false;
		AffectedCardPlay = null;
		InvokeDisplayAmountChanged();
		return;
	}

	private void SummonPet()
	{
		PlayerCmd.AddPet<MegaCrit.Sts2.Core.Models.Monsters.PaelsLegion>(base.Owner);
	}
}
