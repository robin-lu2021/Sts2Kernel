using MegaCrit.Sts2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class SovereignBlade : CardModel
{
	private const int _baseDamage = 10;

	private bool _createdThroughForge;

	private decimal _currentDamage = 10m;

	private decimal _currentRepeats = 1m;

	public override TargetType TargetType
	{
		get
		{
			if (!HasSeekingEdge)
			{
				return TargetType.AnyEnemy;
			}
			return TargetType.AllEnemies;
		}
	}

	private decimal CurrentDamage
	{
		get
		{
			return _currentDamage;
		}
		set
		{
			AssertMutable();
			_currentDamage = value;
		}
	}

	private decimal CurrentRepeats
	{
		get
		{
			return _currentRepeats;
		}
		set
		{
			AssertMutable();
			_currentRepeats = value;
		}
	}

	public bool CreatedThroughForge
	{
		get
		{
			return _createdThroughForge;
		}
		set
		{
			AssertMutable();
			_createdThroughForge = value;
		}
	}

	public override IEnumerable<CardKeyword> CanonicalKeywords => new global::_003C_003Ez__ReadOnlySingleElementList<CardKeyword>(CardKeyword.Retain);

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[5]
	{
		new DamageVar(10m, ValueProp.Move),
		new CalculationBaseVar(0m),
		new CalculationExtraVar(1m),
		new CalculatedVar("SeekingEdgeAmount").WithMultiplier((CardModel card, Creature? _) => (card != null && card.IsMutable && card.Owner != null) ? card.Owner.Creature.GetPowerAmount<SeekingEdgePower>() : 0),
		new RepeatVar(1)
	});

	private bool HasSeekingEdge
	{
		get
		{
			if (base.IsMutable && base.Owner != null)
			{
				return base.Owner.Creature.HasPower<SeekingEdgePower>();
			}
			return false;
		}
	}

	public SovereignBlade()
		: base(2, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		AttackCommand attack = DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).WithHitCount(base.DynamicVars.Repeat.IntValue);
		if (HasSeekingEdge)
		{
			attack = attack.TargetingAllOpponents(base.CombatState).BeforeDamage(delegate
			{
				return;
			});
		}
		else
		{
			ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
			attack = attack.Targeting(cardPlay.Target).BeforeDamage(delegate
			{
				return;
			});
		}
		attack.Execute(choiceContext);
		ParryPower power = base.Owner.Creature.GetPower<ParryPower>();
		if (power != null)
		{
			power.AfterSovereignBladePlayed(base.Owner.Creature, attack.Results);
		}
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}

	protected override void AfterCloned()
	{
		base.AfterCloned();
		CreatedThroughForge = false;
	}

	protected override void AfterDowngraded()
	{
		base.AfterDowngraded();
		base.DynamicVars.Damage.BaseValue = CurrentDamage;
		base.DynamicVars.Repeat.BaseValue = CurrentRepeats;
	}

	public void AddDamage(decimal amount)
	{
		base.DynamicVars.Damage.BaseValue += amount;
		CurrentDamage = base.DynamicVars.Damage.BaseValue;
	}

	public void SetRepeats(decimal amount)
	{
		base.DynamicVars.Repeat.BaseValue = amount;
		CurrentRepeats = base.DynamicVars.Repeat.BaseValue;
	}
}
