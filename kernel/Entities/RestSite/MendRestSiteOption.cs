using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Entities.RestSite;

public sealed class MendRestSiteOption : RestSiteOption
{
	private readonly HealVar _healVar = new HealVar(0m);

	private LocString? _description;

	public override string OptionId => "MEND";

	public override LocString Description
	{
		get
		{
			if (_description == null)
			{
				Player? target = ChooseDefaultTarget(base.Owner);
				_description = base.Description;
				_description.Add("HasTarget", target != null);
				_description.Add("Name", target?.Character.Title.GetFormattedText() ?? string.Empty);
				_healVar.BaseValue = target == null ? 0m : HealRestSiteOption.GetBaseHealAmount(target.Creature);
				_healVar.PreviewValue = target == null ? 0m : GetHealAmount(target);
				_description.Add(_healVar);
			}
			return _description;
		}
	}

	public static decimal GetHealAmount(Player player)
	{
		return Hook.ModifyRestSiteHealAmount(player.RunState, player.Creature, HealRestSiteOption.GetBaseHealAmount(player.Creature));
	}

	public MendRestSiteOption(Player owner)
		: base(owner)
	{
	}

	public override bool OnSelect()
	{
		uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(base.Owner);
		Player? target = null;
		if (LocalContext.IsMe(base.Owner))
		{
			target = ChooseDefaultTarget(base.Owner);
			RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(base.Owner, choiceId, PlayerChoiceResult.FromPlayerId(target?.NetId));
		}
		else
		{
			ulong? num2 = (RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(base.Owner, choiceId)).AsPlayerId();
			if (num2.HasValue)
			{
				target = base.Owner.RunState.GetPlayer(num2.Value);
			}
		}
		if (target != null)
		{
			CreatureCmd.Heal(target.Creature, GetHealAmount(target));
			Hook.AfterRestSiteHeal(target.RunState, target, isMimicked: false);
			return true;
		}
		return false;
	}

	private static Player? ChooseDefaultTarget(Player owner)
	{
		return owner.RunState.Players.Where((Player player) => !LocalContext.IsMe(player)).OrderBy((Player player) => player.NetId).FirstOrDefault();
	}
}
