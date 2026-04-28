using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using AbstractModel = MegaCrit.Sts2.Core.Models.AbstractModel;

namespace MegaCrit.Sts2.Core;

public static class KernelCollectionExtensions
{
	public static IEnumerable<CardModel> Where(this IReadOnlyList<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.Where(cards, predicate);
	}

	public static IEnumerable<CardModel> Where(this IEnumerable<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.Where(cards, predicate);
	}

	public static bool Any(this IReadOnlyList<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.Any(cards, predicate);
	}

	public static bool Any(this IEnumerable<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.Any(cards, predicate);
	}

	public static bool All(this IReadOnlyList<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.All(cards, predicate);
	}

	public static bool All(this IReadOnlyList<PowerModel> powers, Func<PowerModel, bool> predicate)
	{
		return powers.OfType<PowerModel>().All(predicate);
	}

	public static bool Any(this IReadOnlyList<RelicModel> relics, Func<RelicModel, bool> predicate)
	{
		return relics.OfType<RelicModel>().Any(predicate);
	}

	public static bool Any(this IEnumerable<PotionModel> potions, Func<PotionModel, bool> predicate)
	{
		return Enumerable.Any(potions, predicate);
	}

	public static bool Any(this IReadOnlyList<AbstractModel> models, Func<AbstractModel, bool> predicate)
	{
		return models.OfType<AbstractModel>().Any(predicate);
	}

	public static int Count(this IReadOnlyList<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.Count(cards, predicate);
	}

	public static int Count(this IEnumerable<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.Count(cards, predicate);
	}

	public static int Count(this IReadOnlyList<PowerModel> powers, Func<PowerModel, bool> predicate)
	{
		return powers.OfType<PowerModel>().Count(predicate);
	}

	public static IEnumerable<PowerModel> Where(this IReadOnlyList<PowerModel> powers, Func<PowerModel, bool> predicate)
	{
		return powers.OfType<PowerModel>().Where(predicate);
	}

	public static CardModel? FirstOrDefault(this IReadOnlyList<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.FirstOrDefault(cards, predicate);
	}

	public static CardModel? FirstOrDefault(this IEnumerable<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.FirstOrDefault(cards, predicate);
	}

	public static CardModel First(this IEnumerable<CardModel> cards, Func<CardModel, bool> predicate)
	{
		return Enumerable.First(cards, predicate);
	}

	public static RelicModel? FirstOrDefault(this IEnumerable<RelicModel> relics, Func<RelicModel, bool> predicate)
	{
		return Enumerable.FirstOrDefault(relics, predicate);
	}

	public static RelicModel First(this IEnumerable<RelicModel> relics, Func<RelicModel, bool> predicate)
	{
		return Enumerable.First(relics, predicate);
	}

	public static PotionModel? FirstOrDefault(this IEnumerable<PotionModel> potions, Func<PotionModel, bool> predicate)
	{
		return Enumerable.FirstOrDefault(potions, predicate);
	}

	public static CardModel? FirstOrDefault(this IReadOnlyList<CardModel> cards)
	{
		return Enumerable.FirstOrDefault(cards);
	}

	public static List<CardModel> ToList(this IReadOnlyList<CardModel> cards)
	{
		return Enumerable.ToList(cards);
	}

	public static List<CardModel> ToList(this IEnumerable<CardModel> cards)
	{
		return Enumerable.ToList(cards);
	}

	public static IHoverTip ThatDoesDamage(this IHoverTip hoverTip, decimal damage)
	{
		return hoverTip;
	}

	public static IEnumerable<IHoverTip> ThatDoesDamage(this IEnumerable<IHoverTip> hoverTips, decimal damage)
	{
		return hoverTips;
	}

	public static IEnumerable<IHoverTip> ThatDecreasesMaxHp(this IEnumerable<IHoverTip> hoverTips, decimal value)
	{
		return hoverTips;
	}

	public static bool Contains(this IReadOnlyCollection<CardKeyword> keywords, CardKeyword keyword)
	{
		return keywords.Any(k => EqualityComparer<CardKeyword>.Default.Equals(k, keyword));
	}
}
