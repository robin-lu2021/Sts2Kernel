using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Rewards;

public class PotionReward : Reward
{
	private bool _wasTaken;

	protected override RewardType RewardType => RewardType.Potion;

	public override int RewardsSetIndex => 2;

	public PotionModel? Potion { get; private set; }

	public override (float X, float Y) IconPosition => (0f, -2f);

	public override LocString Description => Potion.Title;

	public PotionModel? ClaimedPotion { get; private set; }

	protected override IEnumerable<IHoverTip> ExtraHoverTips => Potion.HoverTips;

	public override bool IsPopulated => Potion != null;

	public PotionReward(Player player)
		: base(player)
	{
	}

	public PotionReward(PotionModel potion, Player player)
		: base(player)
	{
		potion.AssertMutable();
		Potion = potion;
	}

	public override void Populate()
	{
		Rng rng = _rngOverride ?? base.Player.PlayerRng.Rewards;
		if (Potion == null)
		{
			PotionModel potionModel = (Potion = PotionFactory.CreateRandomPotionOutOfCombat(base.Player, rng).ToMutable());
		}
	}

	public override object? CreateIcon()
	{
		return null;
	}

	protected override bool OnSelect()
	{
		PotionProcureResult potionProcureResult = PotionCmd.TryToProcure(Potion, base.Player);
		if (potionProcureResult.success)
		{
			Log.Info($"Obtained {potionProcureResult.potion.Id} from potion reward");
			RunManager.Instance.RewardSynchronizer.SyncLocalObtainedPotion(Potion);
			ClaimedPotion = Potion;
			_wasTaken = true;
			return true;
		}
		if (potionProcureResult.failureReason == PotionProcureFailureReason.TooFull)
		{
			return false;
		}
		ClaimedPotion = Potion;
		_wasTaken = true;
		return true;
	}

	public override void OnSkipped()
	{
		if (!_wasTaken)
		{
			base.Player.RunState.CurrentMapPointHistoryEntry.GetEntry(LocalContext.NetId.Value).PotionChoices.Add(new ModelChoiceHistoryEntry(Potion.Id, wasPicked: false));
			RunManager.Instance.RewardSynchronizer.SyncLocalSkippedPotion(Potion);
		}
	}

	public override void MarkContentAsSeen()
	{
		SaveManager.Instance.MarkPotionAsSeen(Potion);
	}
}
