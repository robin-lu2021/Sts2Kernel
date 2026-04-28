using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core;

public static class ModHelper
{
	public static IEnumerable<T> ConcatModelsFromMods<T>(object source, IEnumerable<T> models)
	{
		return models;
	}

	public static IEnumerable<AbstractModel> IterateAllCombatStateSubscribers(CombatState combatState)
	{
		return new List<AbstractModel>();
	}

	public static IEnumerable<AbstractModel> IterateAllRunStateSubscribers(RunState runState)
	{
		return new List<AbstractModel>();
	}
}
