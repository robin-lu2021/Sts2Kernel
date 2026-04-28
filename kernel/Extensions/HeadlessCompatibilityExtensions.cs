using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.Extensions;

public static class HeadlessCompatibilityExtensions
{
	public static string ToSnakeCase(this string value)
	{
		return StringHelper.SnakeCase(value);
	}

	public static TaskAwaiter<T> GetAwaiter<T>(this T value)
	{
		return Task.FromResult(value).GetAwaiter();
	}

	public static ulong? AsPlayerId(this Task<PlayerChoiceResult> task)
	{
		return task.GetAwaiter().GetResult().AsPlayerId();
	}
}
