using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core;

public static class KernelCardModifierExtensions
{
	public static bool CanEnchant(this EnchantmentModel enchantment, CardModel card)
	{
		return enchantment != null && card != null && card.Enchantment == null;
	}

	public static bool CanAfflict(this AfflictionModel affliction, CardModel card)
	{
		return affliction != null && card != null && card.Affliction == null;
	}
}
