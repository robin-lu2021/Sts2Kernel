using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MegaCrit.Sts2.Core.GameActions.Multiplayer;

public static class INetActionSubtypes
{
	private static readonly Type _t0 = typeof(NetDiscardPotionGameAction);

	private static readonly Type _t1 = typeof(NetEndPlayerTurnAction);

	private static readonly Type _t2 = typeof(NetMoveToMapCoordAction);

	private static readonly Type _t3 = typeof(NetPickRelicAction);

	private static readonly Type _t4 = typeof(NetPlayCardAction);

	private static readonly Type _t5 = typeof(NetReadyToBeginEnemyTurnAction);

	private static readonly Type _t6 = typeof(NetUndoEndPlayerTurnAction);

	private static readonly Type _t7 = typeof(NetUsePotionAction);

	private static readonly Type _t8 = typeof(NetVoteForMapCoordAction);

	private static readonly Type _t9 = typeof(NetVoteToMoveToNextActAction);

	private static readonly Type[] _subtypes = new Type[10]
	{
		_t0, _t1, _t2, _t3, _t4, _t5, _t6, _t7, _t8, _t9
	};

	public static int Count => 10;

	public static IReadOnlyList<Type> All => _subtypes;

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063", Justification = "The list only contains types stored with the correct DynamicallyAccessedMembers attribute, enforced by source generation.")]
	public static Type Get(int i)
	{
		return _subtypes[i];
	}
}
