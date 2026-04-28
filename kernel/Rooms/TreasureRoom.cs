using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Rooms;

public class TreasureRoom : AbstractRoom
{
	private Player? _player;

	public override RoomType RoomType => RoomType.Treasure;

	public override ModelId? ModelId => null;

	public int ActIndex { get; }

	public TreasureRoom(int actIndex)
	{
		if ((actIndex < 0 || actIndex > 2) ? true : false)
		{
			throw new ArgumentOutOfRangeException("actIndex", "must be between 0 and 2");
		}
		ActIndex = actIndex;
	}

	public override void EnterInternal(IRunState? runState, bool isRestoringRoomStackBase)
	{
		if (isRestoringRoomStackBase)
		{
			throw new InvalidOperationException("TreasureRoom does not support room stack reconstruction.");
		}
		_player = LocalContext.GetMe(runState);
		if (runState != null)
		{
			Hook.AfterRoomEntered(runState, this);
		}
	}

	public override void Exit(IRunState? runState)
	{
		RunManager.Instance.TreasureRoomRelicSynchronizer.OnRoomExited();
		return;
	}

	public override void Resume(AbstractRoom _, IRunState? runState)
	{
		throw new NotImplementedException();
	}

	public int DoNormalRewards()
	{
		return RunManager.Instance.OneOffSynchronizer.DoLocalTreasureRoomRewards().GetAwaiter().GetResult();
	}

	public void DoExtraRewardsIfNeeded()
	{
		if (_player == null)
		{
			throw new InvalidOperationException("TreasureRoom has not been entered.");
		}
		RewardsCmd.OfferForRoomEnd(_player, this).GetAwaiter().GetResult();
	}
}
