using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core;

internal static class KernelRuntimeState
{
	private static readonly ConditionalWeakTable<Creature, List<PowerModel>> _powers = new();

	private static readonly ConditionalWeakTable<Player, List<RelicModel>> _relics = new();

	private static readonly ConditionalWeakTable<Player, List<PotionModel?>> _potions = new();

	public static List<PowerModel> GetPowers(Creature creature)
	{
		return _powers.GetValue(creature, static _ => new List<PowerModel>());
	}

	public static List<RelicModel> GetRelics(Player player)
	{
		return _relics.GetValue(player, static _ => new List<RelicModel>());
	}

	public static List<PotionModel?> GetPotions(Player player)
	{
		return _potions.GetValue(player, static p => Enumerable.Repeat<PotionModel?>(null, p.MaxPotionCount).ToList());
	}
}

public static class CreatureKernelExtensions
{
	public static IReadOnlyList<PowerModel> KernelPowers(this Creature creature)
	{
		return KernelRuntimeState.GetPowers(creature);
	}

	public static bool HasPower<T>(this Creature creature) where T : PowerModel
	{
		return creature.KernelPowers().Any(static power => power is T);
	}

	public static T? GetPower<T>(this Creature creature) where T : PowerModel
	{
		return creature.KernelPowers().OfType<T>().FirstOrDefault();
	}

	public static int GetPowerAmount<T>(this Creature creature) where T : PowerModel
	{
		return creature.GetPower<T>()?.Amount ?? 0;
	}

	public static PowerModel? GetPowerById(this Creature creature, string id)
	{
		return creature.KernelPowers().FirstOrDefault((power) => string.Equals(power.Id.Entry, id, StringComparison.OrdinalIgnoreCase));
	}

	public static void ApplyPowerInternal(this Creature creature, PowerModel power)
	{
		List<PowerModel> powers = KernelRuntimeState.GetPowers(creature);
		if (!powers.Contains(power))
		{
			powers.Add(power);
		}
	}

	public static void RemovePowerInternal(this Creature creature, PowerModel power)
	{
		KernelRuntimeState.GetPowers(creature).Remove(power);
	}

	public static IEnumerable<PowerModel> RemoveAllPowersAfterDeath(this Creature creature)
	{
		List<PowerModel> removed = KernelRuntimeState.GetPowers(creature).ToList();
		KernelRuntimeState.GetPowers(creature).Clear();
		foreach (PowerModel power in removed)
		{
			power.RemoveInternal();
		}
		return removed;
	}

	public static void RemoveAllPowersInternalExcept(this Creature creature, params Type[] exceptPowerTypes)
	{
		HashSet<Type> keep = new HashSet<Type>(exceptPowerTypes ?? Array.Empty<Type>());
		List<PowerModel> powers = KernelRuntimeState.GetPowers(creature);
		List<PowerModel> removed = powers.Where((power) => !keep.Contains(power.GetType())).ToList();
		powers.RemoveAll((power) => !keep.Contains(power.GetType()));
		foreach (PowerModel power in removed)
		{
			power.RemoveInternal();
		}
	}
}

public static class PlayerKernelExtensions
{
	public static IReadOnlyList<RelicModel> KernelRelics(this Player player)
	{
		return KernelRuntimeState.GetRelics(player);
	}

	public static T? GetRelic<T>(this Player player) where T : RelicModel
	{
		return player.KernelRelics().OfType<T>().FirstOrDefault();
	}

	public static void AddRelicInternal(this Player player, RelicModel relic, int index = -1, bool silent = false)
	{
		relic.AssertMutable();
		relic.Owner = player;
		List<RelicModel> relics = KernelRuntimeState.GetRelics(player);
		if (index < 0 || index >= relics.Count)
		{
			relics.Add(relic);
		}
		else
		{
			relics.Insert(index, relic);
		}
	}

	public static void RemoveRelicInternal(this Player player, RelicModel relic, bool silent = false)
	{
		if (!KernelRuntimeState.GetRelics(player).Remove(relic))
		{
			throw new InvalidOperationException($"Player does not have relic {relic.Id}");
		}
	}

	public static void MeltRelicInternal(this Player player, RelicModel relic)
	{
		if (!relic.IsWax)
		{
			throw new InvalidOperationException($"{relic.Id} is not wax.");
		}
		if (relic.IsMelted)
		{
			throw new InvalidOperationException($"{relic.Id} is already melted.");
		}
		if (!KernelRuntimeState.GetRelics(player).Contains(relic))
		{
			throw new InvalidOperationException($"Player does not have relic {relic.Id}");
		}
		relic.IsMelted = true;
		relic.Status = RelicStatus.Disabled;
	}

	public static PotionProcureResult AddPotionInternal(this Player player, PotionModel potion, int slotIndex = -1, bool silent = false)
	{
		List<PotionModel?> slots = KernelRuntimeState.GetPotions(player);
		if (slots.Count < player.MaxPotionCount)
		{
			while (slots.Count < player.MaxPotionCount)
			{
				slots.Add(null);
			}
		}
		PotionProcureResult result = new PotionProcureResult
		{
			potion = potion
		};
		if (slotIndex < 0)
		{
			slotIndex = slots.IndexOf(null);
		}
		if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex] != null)
		{
			result.success = false;
			result.failureReason = PotionProcureFailureReason.TooFull;
			return result;
		}
		potion.Owner = player;
		slots[slotIndex] = potion;
		result.success = true;
		return result;
	}

	public static void DiscardPotionInternal(this Player player, PotionModel potion, bool silent = false)
	{
		List<PotionModel?> slots = KernelRuntimeState.GetPotions(player);
		int index = slots.IndexOf(potion);
		if (index >= 0)
		{
			slots[index] = null;
		}
	}
}

public static class ValuePropKernelExtensions
{
	public static bool IsPoweredAttack(this ValueProp props)
	{
		return props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
	}

	public static bool IsCardOrMonsterMove(this ValueProp props)
	{
		return props.HasFlag(ValueProp.Move);
	}
}
