using System;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core;

public static class KernelModelDb
{
	public static CardModel Card<T>() where T : CardModel
	{
		return ModelDb.Card<T>();
	}

	public static MonsterModel Monster<T>() where T : MonsterModel
	{
		return ModelDb.Monster<T>();
	}

	public static PowerModel Power<T>() where T : PowerModel
	{
		return ModelDb.Power<T>();
	}

	public static RelicModel Relic<T>() where T : RelicModel
	{
		return ModelDb.Relic<T>();
	}

	public static PotionModel Potion<T>() where T : PotionModel
	{
		return ModelDb.Potion<T>();
	}
}
