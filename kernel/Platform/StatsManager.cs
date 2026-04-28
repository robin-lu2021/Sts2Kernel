using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Platform;

public static class StatsManager
{
	public static void RefreshGlobalStats()
	{
	}

	public static void IncrementArchitectDamage(int score)
	{
		SaveManager.Instance.Progress.ArchitectDamage += score;
	}

	public static long GetPersonalArchitectDamage()
	{
		return SaveManager.Instance.Progress.ArchitectDamage;
	}

	public static long? GetGlobalArchitectDamage()
	{
		return null;
	}
}
