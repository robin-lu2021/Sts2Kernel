using System;
using MegaCrit.Sts2.Core.Entities.Players;

namespace MegaCrit.Sts2.Core.Entities.Cards;

public static class PileTypeExtensions
{
	public static CardPile GetPile(this PileType pileType, Player player)
	{
		ArgumentNullException.ThrowIfNull(player, "player");
		CardPile? cardPile = CardPile.Get(pileType, player);
		if (cardPile == null)
		{
			throw new InvalidOperationException($"Tried to get {pileType} pile while out of combat.");
		}
		return cardPile;
	}

	public static bool IsCombatPile(this PileType pileType)
	{
		return pileType is PileType.Draw or PileType.Hand or PileType.Discard or PileType.Exhaust or PileType.Play;
	}
}
