using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MegaCrit.Sts2.Core.Models.Afflictions;

public sealed class Devoured : AfflictionModel
{
	private bool _appliedExhaust;

	public bool AppliedExhaust
	{
		get
		{
			return _appliedExhaust;
		}
		set
		{
			AssertMutable();
			_appliedExhaust = value;
		}
	}

	public override bool CanAfflictCardType(CardType cardType)
	{
		if ((uint)(cardType - 1) <= 1u)
		{
			return true;
		}
		return false;
	}
}
