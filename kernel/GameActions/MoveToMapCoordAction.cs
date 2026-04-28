using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.GameActions;

public class MoveToMapCoordAction : GameAction
{
	private readonly Player _player;

	private readonly MapCoord _destination;

	public override ulong OwnerId => _player.NetId;

	public override GameActionType ActionType => GameActionType.NonCombat;

	public MoveToMapCoordAction(Player player, MapCoord destination)
	{
		_player = player;
		_destination = destination;
	}

	protected override Task ExecuteAction()
	{
		RunManager.Instance.EnterMapCoord(_destination);
		return Task.CompletedTask;
	}

	public override INetAction ToNetAction()
	{
		return new NetMoveToMapCoordAction
		{
			destination = _destination
		};
	}

	public override string ToString()
	{
		return $"{"MoveToMapCoordAction"} {_player.NetId} {_destination}";
	}
}
