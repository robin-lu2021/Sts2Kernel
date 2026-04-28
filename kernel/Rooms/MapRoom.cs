using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Rooms;

public class MapRoom : AbstractRoom
{
	public override RoomType RoomType => RoomType.Map;

	public override ModelId? ModelId => null;

	public override void EnterInternal(IRunState? runState, bool isRestoringRoomStackBase)
	{
		return;
	}

	public override void Exit(IRunState? runState)
	{
		return;
	}

	public override void Resume(AbstractRoom _, IRunState? runState)
	{
		return;
	}
}
