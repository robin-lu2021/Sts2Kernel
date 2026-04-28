using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MegaCrit.Sts2.Core.GameActions.Multiplayer;

public interface INetAction : IPacketSerializable
{
	GameAction ToGameAction(Player player);
}
