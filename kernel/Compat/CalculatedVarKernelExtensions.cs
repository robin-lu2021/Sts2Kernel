using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core;

public static class CalculatedVarKernelExtensions
{
	public static CalculatedVar WithMultiplier(this CalculatedVar variable, Func<CardModel, Creature?, decimal> multiplierCalc)
	{
		if (variable == null)
		{
			throw new ArgumentNullException(nameof(variable));
		}
		if (multiplierCalc == null)
		{
			throw new ArgumentNullException(nameof(multiplierCalc));
		}
		return variable.WithMultiplier((card, target) => multiplierCalc(CardModel.FromCore(card), target));
	}
}
