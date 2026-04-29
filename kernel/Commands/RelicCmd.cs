using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using RelicModel = MegaCrit.Sts2.Core.RelicModel;

namespace MegaCrit.Sts2.Core.Commands;

public static class RelicCmd
{
	public static T Obtain<T>(Player player) where T : RelicModel
	{
		return (T)Obtain(KernelModelDb.Relic<T>().ToMutable(), player);
	}

	public static RelicModel Obtain(RelicModel relic, Player player, int index = -1)
	{
		relic.AssertMutable();
		IRunState runState = player.RunState;
		player.AddRelicInternal(relic, index);
		relic.FloorAddedToDeck = runState.TotalFloor;
		relic.AfterObtained();
		return relic;
	}

	public static void Remove(RelicModel relic)
	{
		relic.Owner.RemoveRelicInternal(relic);
		relic.AfterRemoved();
	}

	public static RelicModel Replace(RelicModel original, RelicModel replace)
	{
		original.AssertMutable();
		replace.AssertMutable();
		Player player = original.Owner;
		int indexOfOriginal = -1;
		Remove(original);
		return Obtain(replace, player, indexOfOriginal);
	}

	public static void Melt(RelicModel relic)
	{
		relic.Owner.MeltRelicInternal(relic);
		relic.AfterRemoved();
	}
}
