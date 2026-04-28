using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Achievements;

public class SkillIronclad1Achievement : AchievementModel
{
	private const int _exhaustRequirement = 20;

	private int _cardsExhaustedThisCombat;

	public override void AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (!LocalContext.IsMine(card))
		{
			return;
		}
		_cardsExhaustedThisCombat++;
		if (_cardsExhaustedThisCombat >= 20)
		{
			AchievementsUtil.Unlock(Achievement.CharacterSkillIronclad1, card.Owner);
		}
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		_cardsExhaustedThisCombat = 0;
	}
}
