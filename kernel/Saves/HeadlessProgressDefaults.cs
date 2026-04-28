using System;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Timeline;

namespace MegaCrit.Sts2.Core.Saves;

public static class HeadlessProgressDefaults
{
	private const int MaxAscensionLevel = 10;

	public static void ApplyAllUnlocked(ProgressState progress)
	{
		ArgumentNullException.ThrowIfNull(progress);
		long unlockTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		progress.EnableFtues = false;
		progress.TotalUnlocks = SaveManager.totalAgnosticUnlocks;
		progress.PendingCharacterUnlock = ModelId.none;
		progress.PreferredMultiplayerAscension = MaxAscensionLevel;
		progress.MaxMultiplayerAscension = MaxAscensionLevel;
		foreach (Achievement achievement in Enum.GetValues<Achievement>())
		{
			progress.AddUnlockedAchievement(achievement, unlockTime);
		}
		foreach (var character in ModelDb.AllCharacters)
		{
			CharacterStats stats = progress.GetOrCreateCharacterStats(character.Id);
			stats.MaxAscension = MaxAscensionLevel;
			stats.PreferredAscension = MaxAscensionLevel;
		}
		foreach (var card in ModelDb.AllCards)
		{
			progress.MarkCardAsSeen(card.Id);
		}
		foreach (var relic in ModelDb.AllRelics)
		{
			progress.MarkRelicAsSeen(relic.Id);
		}
		foreach (var potion in ModelDb.AllPotions)
		{
			progress.MarkPotionAsSeen(potion.Id);
		}
		foreach (var eventModel in ModelDb.AllEvents)
		{
			progress.MarkEventAsSeen(ModelDb.GetId(eventModel.GetType()));
		}
		foreach (var act in ModelDb.Acts)
		{
			progress.MarkActAsSeen(act.Id);
		}
		foreach (string epochId in EpochModel.AllEpochIds)
		{
			progress.ObtainEpochOverride(epochId, EpochState.Revealed);
		}
	}
}
