using MegaCrit.Sts2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Shiv : CardModel
{
	public override TargetType TargetType
	{
		get
		{
			if (!HasFanOfKnives)
			{
				return TargetType.AnyEnemy;
			}
			return TargetType.AllEnemies;
		}
	}

	protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Shiv };

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DamageVar(4m, ValueProp.Move));

	public override IEnumerable<CardKeyword> CanonicalKeywords => new global::_003C_003Ez__ReadOnlySingleElementList<CardKeyword>(CardKeyword.Exhaust);

	private bool HasFanOfKnives
	{
		get
		{
			if (base.IsMutable && base.Owner != null)
			{
				return base.Owner.Creature.HasPower<FanOfKnivesPower>();
			}
			return false;
		}
	}

	public Shiv()
		: base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		AttackCommand attackCommand = DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this);
		if (HasFanOfKnives)
		{
			Creature lastEnemy = base.CombatState.HittableEnemies.LastOrDefault();
			attackCommand = attackCommand.TargetingAllOpponents(base.CombatState);
		}
		else
		{
			ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
			attackCommand = attackCommand.Targeting(cardPlay.Target);
		}
		if (base.Owner.Character is Silent)
		{
		}
		attackCommand.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(2m);
	}

	public static CardModel? CreateInHand(Player owner, CombatState combatState)
	{
		return (CreateInHand(owner, 1, combatState).FirstOrDefault());
	}

	public static IEnumerable<CardModel> CreateInHand(Player owner, int count, CombatState combatState)
	{
		if (count == 0)
		{
			return Array.Empty<CardModel>();
		}
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return Array.Empty<CardModel>();
		}
		List<CardModel> shivs = new List<CardModel>();
		for (int i = 0; i < count; i++)
		{
			shivs.Add(combatState.CreateCard<Shiv>(owner));
		}
		CardPileCmd.AddGeneratedCardsToCombat(shivs, PileType.Hand, addedByPlayer: true);
		return shivs;
	}
}
