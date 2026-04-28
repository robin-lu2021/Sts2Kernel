using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;

namespace MegaCrit.Sts2.Core;

public static class KernelHoverTipFactory
{
	public static IEnumerable<IHoverTip> FromCardWithCardHoverTips<T>(bool inheritsUpgrades = false) where T : CardModel
	{
		return HoverTipFactory.FromCardWithCardHoverTips<T>(inheritsUpgrades);
	}

	public static IHoverTip FromCard<T>(bool upgrade = false) where T : CardModel
	{
		return HoverTipFactory.FromCard<T>(upgrade);
	}

	public static IHoverTip FromCard(CardModel card, bool upgrade = false)
	{
		return HoverTipFactory.FromCard(card, upgrade);
	}

	public static IEnumerable<IHoverTip> FromRelic<T>() where T : RelicModel
	{
		return HoverTipFactory.FromRelic<T>();
	}

	public static IEnumerable<IHoverTip> FromRelicExcludingItself<T>() where T : RelicModel
	{
		return HoverTipFactory.FromRelicExcludingItself<T>();
	}

	public static IEnumerable<IHoverTip> FromRelic(RelicModel relic)
	{
		return HoverTipFactory.FromRelic(relic);
	}

	public static IHoverTip FromPotion<T>() where T : PotionModel
	{
		return HoverTipFactory.FromPotion<T>();
	}

	public static IHoverTip FromPotion(PotionModel potion)
	{
		return HoverTipFactory.FromPotion(potion);
	}

	public static IHoverTip FromPower<T>() where T : PowerModel
	{
		return HoverTipFactory.FromPower<T>();
	}

	public static IEnumerable<IHoverTip> FromPowerWithPowerHoverTips<T>() where T : PowerModel
	{
		return HoverTipFactory.FromPowerWithPowerHoverTips<T>();
	}

	public static IHoverTip FromPower(PowerModel power)
	{
		return HoverTipFactory.FromPower(power);
	}
}
