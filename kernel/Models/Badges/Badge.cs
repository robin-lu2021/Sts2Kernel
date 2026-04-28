using System.Linq;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Models.Badges;

public abstract class Badge
{
	protected readonly SerializableRun _run;

	protected readonly SerializablePlayer _localPlayer;

	public virtual string Id => "NOT_SET";

	public virtual BadgeRarity Rarity => BadgeRarity.None;

	public abstract bool RequiresWin { get; }

	public abstract bool MultiplayerOnly { get; }

	protected Badge(SerializableRun run, ulong playerId)
	{
		_run = run;
		_localPlayer = _run.Players.First((SerializablePlayer p) => p.NetId == playerId);
	}

	public abstract bool IsObtained();

	public SerializableBadge ToSerializable()
	{
		return new SerializableBadge
		{
			Id = Id,
			Rarity = Rarity
		};
	}
}
