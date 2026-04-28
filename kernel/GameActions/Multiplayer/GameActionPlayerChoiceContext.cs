using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.GameActions.Multiplayer;

public class GameActionPlayerChoiceContext : PlayerChoiceContext
{
	public GameAction Action { get; }

	public GameActionPlayerChoiceContext(GameAction action)
	{
		Action = action;
	}

	public override void SignalPlayerChoiceBegun(PlayerChoiceOptions options)
	{
		if (Program.UseSynchronousPlayerChoiceContexts)
		{
			return;
		}
		if (RunManager.Instance.ActionExecutor.CurrentlyRunningAction != Action)
		{
			Log.Error($"Tried to interrupt shared queue action {RunManager.Instance.ActionExecutor.CurrentlyRunningAction} with a player choice context with action {Action}!");
			return;
		}
		RunManager.Instance.ActionQueueSet.PauseActionForPlayerChoice(Action, options);
	}

	public override void SignalPlayerChoiceEnded()
	{
		if (Program.UseSynchronousPlayerChoiceContexts)
		{
			return;
		}
		if (Action.OwnerId == LocalContext.NetId)
		{
			RunManager.Instance.ActionQueueSynchronizer.RequestResumeActionAfterPlayerChoice(Action);
		}
		Action.WaitForActionToResumeExecutingAfterPlayerChoice();
	}
}
