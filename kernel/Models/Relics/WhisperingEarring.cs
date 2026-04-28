using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class WhisperingEarring : RelicModel
{
	private const int _maxCardsToPlay = 13;

	public override RelicRarity Rarity => RelicRarity.Ancient;


	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new EnergyVar(1));

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != base.Owner)
		{
			return amount;
		}
		return amount + base.DynamicVars.Energy.BaseValue;
	}

	public override void BeforePlayPhaseStartLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner)
		{
			return;
		}
		CombatState combatState = player.Creature.CombatState;
		if (combatState.RoundNumber > 1)
		{
			return;
		}
		 
		bool flag;
		using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
		{
			int cardsPlayed;
			for (cardsPlayed = 0; cardsPlayed < 13; cardsPlayed++)
			{
				if (CombatManager.Instance.IsOverOrEnding)
				{
					break;
				}
				if (CombatManager.Instance.IsPlayerReadyToEndTurn(player))
				{
					break;
				}
				CardPile pile = PileType.Hand.GetPile(base.Owner);
				CardModel? card = pile.Cards.OfType<CardModel>().FirstOrDefault((CardModel c) => c.CanPlay());
				if (card == null)
				{
					break;
				}
				Creature target = GetTarget(card, combatState);
				card.SpendResources();
				CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
			}
			flag = cardsPlayed >= 13;
			if (cardsPlayed == 0)
			{
				return;
			}
		}
	}

	private Creature? GetTarget(CardModel card, CombatState combatState)
	{
		Rng combatTargets = base.Owner.RunState.Rng.CombatTargets;
		return card.TargetType switch
		{
			TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(), 
			TargetType.AnyAlly => combatTargets.NextItem(combatState.Allies.Where((Creature c) => c != null && c.IsAlive && c.IsPlayer && c != base.Owner.Creature)), 
			TargetType.AnyPlayer => base.Owner.Creature, 
			_ => null, 
		};
	}
}
