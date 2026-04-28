using System;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Multiplayer.Game;

public class FlavorSynchronizer : IDisposable
{
	private const ulong _pingDebounceMsec = 1000uL;

	private const ulong _mapPingDebounceMsec = 200uL;

	private readonly INetGameService _gameService;

	private long _nextAllowedPingTime;

	public event Action<ulong>? OnEndTurnPingReceived;

	public FlavorSynchronizer(INetGameService gameService, IPlayerCollection playerCollection, ulong localPlayerId)
	{
		_gameService = gameService;
		_gameService.RegisterMessageHandler<EndTurnPingMessage>(HandleEndTurnPingMessage);
		_gameService.RegisterMessageHandler<MapPingMessage>(HandleMapPingMessage);
	}

	public void Dispose()
	{
		_gameService.UnregisterMessageHandler<EndTurnPingMessage>(HandleEndTurnPingMessage);
		_gameService.UnregisterMessageHandler<MapPingMessage>(HandleMapPingMessage);
	}

	public void SendEndTurnPing()
	{
		long now = Environment.TickCount64;
		if (now >= _nextAllowedPingTime)
		{
			_gameService.SendMessage(default(EndTurnPingMessage));
			_nextAllowedPingTime = now + 1000;
		}
	}

	public void SendMapPing(MapCoord coord)
	{
		long now = Environment.TickCount64;
		if (now >= _nextAllowedPingTime)
		{
			_gameService.SendMessage(new MapPingMessage
			{
				coord = coord
			});
			_nextAllowedPingTime = now + 200;
		}
	}

	private void HandleEndTurnPingMessage(EndTurnPingMessage message, ulong senderId)
	{
		this.OnEndTurnPingReceived?.Invoke(senderId);
	}

	private void HandleMapPingMessage(MapPingMessage message, ulong senderId)
	{
		_ = message;
		_ = senderId;
	}
}
