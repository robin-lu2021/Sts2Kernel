using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Logging;

namespace MegaCrit.Sts2.Core.Helpers;

public static class TaskHelper
{
	public static Task RunSafely(Task task)
	{
		task.ContinueWith(t =>
		{
			if (t.IsFaulted && t.Exception?.InnerException is not TaskCanceledException)
			{
				Log.Error(t.Exception!.ToString());
			}
		}, TaskContinuationOptions.OnlyOnFaulted);
		return task;
	}

	public static int WhenAny(params Task[] tasks)
	{
		return Task.WaitAny(tasks);
	}
}
