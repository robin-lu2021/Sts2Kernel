using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Achievements;

public class SkillSilent1Achievement : AchievementModel
{
	private CardModel? _firstCardOnStack;

	private int _slyCardsPlayed;

	public override void BeforeCardPlayed(CardPlay cardPlay)
	{
		if (!LocalContext.IsMine(cardPlay.Card))
		{
			return;
		}
		if (_firstCardOnStack == null)
		{
			_firstCardOnStack = cardPlay.Card;
		}
	}

	public override void BeforeCardAutoPlayed(CardModel card, Creature? target, AutoPlayType type)
	{
		if (!LocalContext.IsMine(card))
		{
			return;
		}
		if (type != AutoPlayType.SlyDiscard)
		{
			return;
		}
		if (_firstCardOnStack == null)
		{
			return;
		}
		_slyCardsPlayed++;
		if (_slyCardsPlayed >= 5)
		{
			AchievementsUtil.Unlock(Achievement.CharacterSkillSilent1, card.Owner);
		}
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!LocalContext.IsMine(cardPlay.Card))
		{
			return;
		}
		if (cardPlay.Card == _firstCardOnStack)
		{
			_firstCardOnStack = null;
			_slyCardsPlayed = 0;
		}
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		_firstCardOnStack = null;
		_slyCardsPlayed = 0;
	}
}
