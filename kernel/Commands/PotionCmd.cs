using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core.Commands;

public static class PotionCmd
{
	public static PotionProcureResult TryToProcure<T>(Player player) where T : PotionModel
	{
		return TryToProcure(ModelDb.Potion<T>().ToMutable(), player);
	}

	public static PotionProcureResult TryToProcure(PotionModel potion, Player player, int slotIndex = -1)
	{
		potion.AssertMutable();
		PotionProcureResult result = player.AddPotionInternal(potion, slotIndex);
		return result;
	}

	public static void Discard(PotionModel potion)
	{
		potion.Discard();
		potion.Owner.DiscardPotionInternal(potion);
	}
}
