using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;

namespace MegaCrit.Sts2.Core.Entities.RestSite;

public sealed class HealRestSiteOption : RestSiteOption
{
	public override string OptionId => "HEAL";

	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			HealVar dynamicVar = new HealVar(GetBaseHealAmount(base.Owner.Creature))
			{
				PreviewValue = GetHealAmount(base.Owner)
			};
			description.Add("Character", base.Owner.Character.Id.Entry);
			description.Add(dynamicVar);
			IReadOnlyList<LocString> source = Hook.ModifyExtraRestSiteHealText(base.Owner.RunState, base.Owner, Array.Empty<LocString>());
			if (source.Any())
			{
				description.Add("ExtraText", "\n" + string.Join("\n", source.Select((LocString s) => s.GetFormattedText())));
			}
			else
			{
				description.Add("ExtraText", string.Empty);
			}
			return description;
		}
	}

	public static decimal GetHealAmount(Player player)
	{
		return Hook.ModifyRestSiteHealAmount(player.RunState, player.Creature, GetBaseHealAmount(player.Creature));
	}

	public HealRestSiteOption(Player owner)
		: base(owner)
	{
	}

	public override bool OnSelect()
	{
		ExecuteRestSiteHeal(base.Owner, isMimicked: false);
		return true;
	}
	
	public static decimal GetBaseHealAmount(Creature creature)
	{
		return (decimal)creature.MaxHp * 0.3m;
	}

	public static void ExecuteRestSiteHeal(Player player, bool isMimicked)
	{
		CreatureCmd.Heal(player.Creature, GetHealAmount(player));
		Hook.AfterRestSiteHeal(player.RunState, player, isMimicked);
		List<Reward> rewards = new List<Reward>();
		Hook.ModifyRestSiteHealRewards(player.RunState, player, rewards, isMimicked);
		RewardsCmd.OfferCustom(player, rewards);
	}
}
