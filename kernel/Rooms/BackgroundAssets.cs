using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Rooms;

public class BackgroundAssets
{
	public string BackgroundScenePath { get; }

	public List<string> BgLayers { get; }

	public string? FgLayer { get; }

	public IEnumerable<string> AssetPaths => from s in new string[2]
		{
			BackgroundScenePath,
			FgLayer ?? string.Empty
		}.Concat(BgLayers)
		where !string.IsNullOrWhiteSpace(s)
		select s;

	public BackgroundAssets(string title, Rng rng)
	{
		_ = rng;
		BackgroundScenePath = SceneHelper.GetScenePath($"backgrounds/{title}/{title}_background");
		BgLayers = new List<string>();
		FgLayer = null;
	}
}
