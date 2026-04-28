using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;

namespace MegaCrit.Sts2.Core.Entities.RestSite;

public abstract class RestSiteOption
{
	public abstract string OptionId { get; }

	protected Player Owner { get; }

	public LocString Title => new LocString("rest_site_ui", "OPTION_" + OptionId + ".name");

	public virtual LocString Description => new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");

	public string IconPath => ImageHelper.GetImagePath("ui/rest_site/option_" + OptionId.ToLowerInvariant() + ".png");

	public virtual IEnumerable<string> AssetPaths => new[] { IconPath };

	public bool IsEnabled { get; set; } = true;

	protected RestSiteOption(Player owner)
	{
		Owner = owner;
	}

	public static List<RestSiteOption> Generate(Player player)
	{
		int num = 2;
		List<RestSiteOption> list = new List<RestSiteOption>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<RestSiteOption> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = new HealRestSiteOption(player);
		num2++;
		span[num2] = new SmithRestSiteOption(player);
		List<RestSiteOption> list2 = list;
		if (player.RunState.Players.Count > 1)
		{
			list2.Add(new MendRestSiteOption(player));
		}
		Hook.ModifyRestSiteOptions(player.RunState, player, list2);
		return list2;
	}

	public abstract bool OnSelect();

	public override bool Equals(object? obj)
	{
		if (obj is RestSiteOption restSiteOption && OptionId == restSiteOption.OptionId)
		{
			return Owner == restSiteOption.Owner;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (OptionId, Owner).GetHashCode();
	}

	public static bool operator ==(RestSiteOption? left, RestSiteOption? right)
	{
		if ((object)left == right)
		{
			return true;
		}
		if ((object)left == null)
		{
			if ((object)left == null)
			{
				return (object)right == null;
			}
			return false;
		}
		return left.Equals(right);
	}

	public static bool operator !=(RestSiteOption? left, RestSiteOption? right)
	{
		return !(left == right);
	}
}
