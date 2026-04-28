using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Commands;

public static class RewardsCmd
{
	public static Task OfferForRoomEnd(Player player, AbstractRoom room)
	{
		RewardsSet rewardsSet;
		if (room is CombatRoom combatRoom)
		{
			EncounterModel encounter = combatRoom.Encounter;
			if (encounter != null && !encounter.ShouldGiveRewards)
			{
				rewardsSet = new RewardsSet(player).EmptyForRoom(room);
				goto IL_00b2;
			}
		}
		rewardsSet = new RewardsSet(player).WithRewardsFromRoom(room);
		goto IL_00b2;
		IL_00b2:
		rewardsSet.Offer().GetAwaiter().GetResult();
		return Task.CompletedTask;
	}

	public static void OfferCustom(Player player, List<Reward> rewards)
	{
		new RewardsSet(player).WithCustomRewards(rewards).Offer().GetAwaiter().GetResult();
	}

	public static List<Reward> GenerateForRoomEndDebug(Player player, AbstractRoom room)
	{
		return new RewardsSet(player).WithRewardsFromRoom(room).GenerateWithoutOffering().GetAwaiter().GetResult();
	}

	public static List<Reward> GenerateCustomDebug(Player player, List<Reward> rewards)
	{
		return new RewardsSet(player).WithCustomRewards(rewards).GenerateWithoutOffering().GetAwaiter().GetResult();
	}
}
