using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Multiplayer.Messages;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Checksums;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;

namespace MegaCrit.Sts2.Core.Multiplayer.Serialization;

public static class INetMessageSubtypes
{
	private static readonly Type _t0 = typeof(ActionEnqueuedMessage);

	private static readonly Type _t1 = typeof(CardRemovedMessage);

	private static readonly Type _t2 = typeof(ChecksumDataMessage);

	private static readonly Type _t3 = typeof(StateDivergenceMessage);

	private static readonly Type _t4 = typeof(EndTurnPingMessage);

	private static readonly Type _t5 = typeof(MapPingMessage);

	private static readonly Type _t6 = typeof(RestSiteOptionHoveredMessage);

	private static readonly Type _t7 = typeof(HookActionEnqueuedMessage);

	private static readonly Type _t8 = typeof(MerchantCardRemovalMessage);

	private static readonly Type _t9 = typeof(PaelsWingSacrificeMessage);

	private static readonly Type _t10 = typeof(PlayerChoiceMessage);

	private static readonly Type _t11 = typeof(RequestEnqueueActionMessage);

	private static readonly Type _t12 = typeof(RequestEnqueueHookActionMessage);

	private static readonly Type _t13 = typeof(RequestResumeActionAfterPlayerChoiceMessage);

	private static readonly Type _t14 = typeof(ResumeActionAfterPlayerChoiceMessage);

	private static readonly Type _t15 = typeof(RunAbandonedMessage);

	private static readonly Type _t16 = typeof(GoldLostMessage);

	private static readonly Type _t17 = typeof(OptionIndexChosenMessage);

	private static readonly Type _t18 = typeof(PeerInputMessage);

	private static readonly Type _t19 = typeof(RewardObtainedMessage);

	private static readonly Type _t20 = typeof(SharedEventOptionChosenMessage);

	private static readonly Type _t21 = typeof(VotedForSharedEventOptionMessage);

	private static readonly Type _t22 = typeof(SyncPlayerDataMessage);

	private static readonly Type _t23 = typeof(SyncRngMessage);

	private static readonly Type _t24 = typeof(TreasureChestOpenedMessage);

	private static readonly Type _t25 = typeof(HeartbeatRequestMessage);

	private static readonly Type _t26 = typeof(HeartbeatResponseMessage);

	private static readonly Type[] _subtypes = new Type[27]
	{
		_t0, _t1, _t2, _t3, _t4, _t5, _t6, _t7, _t8, _t9,
		_t10, _t11, _t12, _t13, _t14, _t15, _t16, _t17, _t18, _t19,
		_t20, _t21, _t22, _t23, _t24, _t25, _t26
	};

	public static int Count => 27;

	public static IReadOnlyList<Type> All => _subtypes;

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063", Justification = "The list only contains types stored with the correct DynamicallyAccessedMembers attribute, enforced by source generation.")]
	public static Type Get(int i)
	{
		return _subtypes[i];
	}
}
