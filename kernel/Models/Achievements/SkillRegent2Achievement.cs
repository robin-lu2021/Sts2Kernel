using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Platform;

namespace MegaCrit.Sts2.Core.Models.Achievements;

public class SkillRegent2Achievement : AchievementModel
{
	private const int _starThreshold = 20;

	public override void AfterStarsGained(int amount, Player gainer)
	{
		if (!LocalContext.IsMe(gainer))
		{
			return;
		}
		PlayerCombatState? playerCombatState = gainer.PlayerCombatState;
		if (playerCombatState != null && playerCombatState.Stars < 20)
		{
			return;
		}
		AchievementsUtil.Unlock(Achievement.CharacterSkillRegent2, gainer);
	}
}
