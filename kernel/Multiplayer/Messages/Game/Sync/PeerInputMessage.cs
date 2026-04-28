using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;

public class PeerInputMessage : INetMessage, IPacketSerializable
{
	public bool mouseDown;

	public bool isTargeting;

	public NetScreenType screenType;

	public HoveredModelData hoveredModelData;

	public bool isUsingController;

	public bool ShouldBroadcast => true;

	public NetTransferMode Mode => NetTransferMode.Unreliable;

	public LogLevel LogLevel => LogLevel.VeryDebug;

	public void Serialize(PacketWriter writer)
	{
		writer.WriteBool(mouseDown);
		writer.WriteBool(isTargeting);
		writer.WriteEnum(screenType);
		writer.Write(hoveredModelData);
		writer.WriteBool(isUsingController);
	}

	public void Deserialize(PacketReader reader)
	{
		mouseDown = reader.ReadBool();
		isTargeting = reader.ReadBool();
		screenType = reader.ReadEnum<NetScreenType>();
		hoveredModelData = reader.Read<HoveredModelData>();
		isUsingController = reader.ReadBool();
	}
}
