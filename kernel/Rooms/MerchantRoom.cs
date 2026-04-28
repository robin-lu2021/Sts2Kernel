using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Rooms;

public class MerchantRoom : AbstractRoom
{
	private static MerchantDialogueSet? _dialogue;

	public override RoomType RoomType => RoomType.Shop;

	public MerchantInventory Inventory { get; private set; }

	public override ModelId? ModelId => null;

	public static MerchantDialogueSet Dialogue
	{
		get
		{
			if (_dialogue != null)
			{
				return _dialogue;
			}
			LocTable table = LocManager.Instance.GetTable("merchant_room");
			IReadOnlyList<LocString> locStringsWithPrefix = table.GetLocStringsWithPrefix("MERCHANT.talk.");
			_dialogue = MerchantDialogueSet.CreateFromLocStrings(locStringsWithPrefix);
			return _dialogue;
		}
	}

	public override void EnterInternal(IRunState? runState, bool isRestoringRoomStackBase)
	{
		if (isRestoringRoomStackBase)
		{
			throw new InvalidOperationException("MerchantRoom does not support room stack reconstruction.");
		}
		Inventory = MerchantInventory.CreateForNormalMerchant(LocalContext.GetMe(runState));
		if (runState != null)
		{
			Hook.AfterRoomEntered(runState, this);
		}
	}

	public override void Exit(IRunState? runState)
	{
		return;
	}

	public override void Resume(AbstractRoom _, IRunState? runState)
	{
		throw new NotImplementedException();
	}
}
