using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace MegaCrit.Sts2.Core.Multiplayer.Serialization;

public interface INetMessage : IPacketSerializable
{
	bool ShouldBroadcast { get; }

	NetTransferMode Mode { get; }

	LogLevel LogLevel { get; }
}
