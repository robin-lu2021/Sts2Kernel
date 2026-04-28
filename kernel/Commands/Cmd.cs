using System.Threading.Tasks;

namespace MegaCrit.Sts2.Core.Commands;

public static class Cmd
{
	public static Task Wait(float seconds)
	{
		return Task.CompletedTask;
	}

	public static Task CustomScaledWait(float fastModeDuration, float normalDuration)
	{
		return Task.CompletedTask;
	}
}
