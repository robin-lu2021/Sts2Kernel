using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;

namespace MegaCrit.Sts2.Core.Commands.Builders;

public sealed class AttackContext : IDisposable
{
	private readonly CombatState _combatState;

	private readonly AttackCommand _attackCommand;

	private bool _disposed;

	private AttackContext(CombatState combatState, CardModel cardSource)
	{
		_combatState = combatState;
		_attackCommand = new AttackCommand(0m).FromCard(cardSource).TargetingAllOpponents(combatState);
	}

	public static AttackContext CreateAsync(CombatState combatState, CardModel cardSource)
	{
		return new AttackContext(combatState, cardSource);
	}

	public void AddHit(IEnumerable<DamageResult> results)
	{
		_attackCommand.IncrementHitsInternal();
		_attackCommand.AddResultsInternal(results);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		try
		{
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
	}
}
