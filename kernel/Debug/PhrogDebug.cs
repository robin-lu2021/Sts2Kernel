using System;
using MegaCrit.Sts2.Core.Logging;

namespace MegaCrit.Sts2.Core.Debug;

internal static class PhrogDebug
{
	public static bool IsEnabled => string.Equals(Environment.GetEnvironmentVariable("STS2_PHROG_DEBUG"), "1", StringComparison.Ordinal);

	public static void LogInfo(string message)
	{
		if (IsEnabled)
		{
			Log.Info("[PHROG_DEBUG] " + message);
		}
	}
}
