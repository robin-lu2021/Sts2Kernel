using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Commands;

public static class OstyCmd
{
	public static SummonResult Summon(PlayerChoiceContext choiceContext, Player summoner, decimal amount, AbstractModel? source)
	{
		CombatState combatState = summoner.Creature.CombatState;
		amount = Hook.ModifySummonAmount(combatState, summoner, amount, null);
		if (amount == 0m)
		{
			return new SummonResult(summoner.Osty, 0m);
		}
		if (CombatManager.Instance.IsInProgress)
		{
		}
		Creature osty = combatState.Allies.FirstOrDefault((Creature c) => c.Monster is Osty && c.PetOwner == summoner);
		if (summoner.IsOstyAlive)
		{
			CreatureCmd.GainMaxHp(summoner.Osty, amount);
		}
		else
		{
			bool isReviving = osty != null;
			if (isReviving)
			{
				if (osty.IsAlive)
				{
					throw new InvalidOperationException("We shouldn't make it here if Osty is still alive!");
				}
				summoner.PlayerCombatState.AddPetInternal(osty);
			}
			else
			{
				osty = PlayerCmd.AddPet<Osty>(summoner);
				PowerCmd.Apply<DieForYouPower>(osty, 1m, null, null);
			}
			CreatureCmd.SetMaxHp(osty, amount);
			CreatureCmd.Heal(osty, amount, isReviving);
			if (isReviving)
			{
				Hook.AfterOstyRevived(combatState, osty);
			}
		}
		if (TestMode.IsOff)
		{
		}
		CombatManager.Instance.History.Summoned(combatState, (int)amount, summoner);
		Hook.AfterSummon(combatState, choiceContext, summoner, amount);
		return new SummonResult(summoner.Osty, amount);
	}
}
