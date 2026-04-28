using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class BansheesCry : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DamageVar(33m, ValueProp.Move),
		new EnergyVar(2)
	});

	public BansheesCry()
		: base(9, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(base.CombatState)
			
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-2);
	}

	public override void AfterCardEnteredCombat(CardModel card)
	{
		if (card != this)
		{
			return;
		}
		if (base.IsClone)
		{
			return;
		}
		int num = CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry e) => e.WasEthereal && e.CardPlay.Card.Owner == base.Owner);
		base.EnergyCost.AddThisCombat(-num * base.DynamicVars.Energy.IntValue);
		return;
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner)
		{
			return;
		}
		if (!cardPlay.Card.Keywords.Contains(CardKeyword.Ethereal))
		{
			return;
		}
		base.EnergyCost.AddThisCombat(-base.DynamicVars.Energy.IntValue);
		return;
	}
}
