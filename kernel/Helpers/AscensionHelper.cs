using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Helpers;

public static class AscensionHelper
{
	private sealed class ScopedOverride : IDisposable
	{
		public void Dispose()
		{
			if (_overrideAscensionLevels.Count > 0)
			{
				_overrideAscensionLevels.Pop();
			}
		}
	}

	private static readonly Stack<int> _overrideAscensionLevels = new Stack<int>();

	public static double PovertyAscensionGoldMultiplier => 0.75;

	public static IDisposable PushOverride(int ascensionLevel)
	{
		_overrideAscensionLevels.Push(Math.Max(0, ascensionLevel));
		return new ScopedOverride();
	}

	public static int GetValueIfAscension(AscensionLevel level, int ascensionValue, int fallbackValue)
	{
		if (!HasAscension(level))
		{
			return fallbackValue;
		}
		return ascensionValue;
	}

	public static float GetValueIfAscension(AscensionLevel level, float ascensionValue, float fallbackValue)
	{
		if (!HasAscension(level))
		{
			return fallbackValue;
		}
		return ascensionValue;
	}

	public static decimal GetValueIfAscension(AscensionLevel level, decimal ascensionValue, decimal fallbackValue)
	{
		if (!HasAscension(level))
		{
			return fallbackValue;
		}
		return ascensionValue;
	}

	public static bool HasAscension(AscensionLevel level)
	{
		if (_overrideAscensionLevels.Count > 0)
		{
			return _overrideAscensionLevels.Peek() >= (int)level;
		}
		return RunManager.Instance.HasAscension(level);
	}

	public static LocString GetTitle(int level)
	{
		return new LocString("ascension", "LEVEL_" + GetKey(level) + ".title");
	}

	public static LocString GetDescription(int level)
	{
		return new LocString("ascension", "LEVEL_" + GetKey(level) + ".description");
	}

	private static string GetKey(int level)
	{
		return level.ToString("D2");
	}
}
