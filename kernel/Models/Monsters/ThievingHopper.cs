using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class ThievingHopper : MonsterModel
{
	private static readonly Func<CardModel, bool>[] _stealPriorities = new Func<CardModel, bool>[4]
	{
		(CardModel c) => !(c.Enchantment is Imbued) && c.Rarity == CardRarity.Uncommon,
		delegate(CardModel c)
		{
			bool flag = !(c.Enchantment is Imbued);
			bool flag2 = flag;
			if (flag2)
			{
				bool flag3;
				switch (c.Rarity)
				{
				case CardRarity.Common:
				case CardRarity.Rare:
				case CardRarity.Event:
					flag3 = true;
					break;
				default:
					flag3 = false;
					break;
				}
				flag2 = flag3;
			}
			return flag2;
		},
		delegate(CardModel c)
		{
			bool flag = !(c.Enchantment is Imbued);
			bool flag2 = flag;
			if (flag2)
			{
				CardRarity rarity = c.Rarity;
				bool flag3 = ((rarity == CardRarity.Basic || rarity == CardRarity.Quest) ? true : false);
				flag2 = flag3;
			}
			return flag2;
		},
		(CardModel c) => c.Rarity == CardRarity.Ancient || c.Enchantment is Imbued
	};

	public const string stunTrigger = "StunTrigger";

	private bool _isHovering;

	private const string _fleeTrigger = "Flee";

	private const string _hoverTrigger = "Hover";

	private const string _stealTrigger = "Steal";

	private const string _escapeMoveId = "ESCAPE_MOVE";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 84, 79);

	public override int MaxInitialHp => MinInitialHp;

	public bool IsHovering
	{
		get
		{
			return _isHovering;
		}
		set
		{
			AssertMutable();
			_isHovering = value;
		}
	}

	private int TheftDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 17);

	private int HatTrickDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 23, 21);

	private int NabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<EscapeArtistPower>(base.Creature, 5m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("THIEVERY_MOVE", SyncMove(ThieveryMove), new SingleAttackIntent(TheftDamage), new CardDebuffIntent());
		MoveState moveState2 = new MoveState("NAB_MOVE", SyncMove(NabMove), new SingleAttackIntent(NabDamage));
		MoveState moveState3 = new MoveState("HAT_TRICK_MOVE", SyncMove(HatTrickMove), new SingleAttackIntent(HatTrickDamage));
		MoveState moveState4 = new MoveState("FLUTTER_MOVE", SyncMove(FlutterMove), new BuffIntent());
		MoveState moveState5 = new MoveState("ESCAPE_MOVE", SyncMove(EscapeMove), new EscapeIntent());
		moveState.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState5;
		moveState5.FollowUpState = moveState5;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		list.Add(moveState5);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void ThieveryMove(IReadOnlyList<Creature> targets)
	{
		List<CardModel> cardsToSteal = new List<CardModel>();
		foreach (Creature target in targets)
		{
			if (target.IsDead)
			{
				continue;
			}
			List<CardModel> list = (from c in CardPile.GetCards(target.Player ?? target.PetOwner, PileType.Draw, PileType.Discard)
				where c.DeckVersion != null
				select c).ToList();
			IEnumerable<CardModel> items = list;
			Func<CardModel, bool>[] stealPriorities = _stealPriorities;
			foreach (Func<CardModel, bool> predicate in stealPriorities)
			{
				IEnumerable<CardModel> enumerable = list.Where(predicate);
				if (enumerable.Any())
				{
					items = enumerable;
					break;
				}
			}
			CardModel cardToSteal = base.RunRng.CombatCardGeneration.NextItem(items);
			CardPileCmd.RemoveFromCombat(cardToSteal);
			cardsToSteal.Add(cardToSteal);
		}
		foreach (CardModel item in cardsToSteal)
		{
			SwipePower swipe = (SwipePower)KernelModelDb.Power<SwipePower>().ToMutable();
			swipe.Steal(item);
			PowerCmd.Apply(swipe, base.Creature, 1m, base.Creature, null);
		}
		DamageCmd.Attack(TheftDamage).FromMonster(this).WithNoAttackerAnim()
			.Execute(null);
	}

	private void NabMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(NabDamage).FromMonster(this)
			.Execute(null);
	}

	private void HatTrickMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(HatTrickDamage).FromMonster(this)
			.Execute(null);
	}

	private void FlutterMove(IReadOnlyList<Creature> targets)
	{
		IsHovering = true;
		PowerCmd.Apply<FlutterPower>(base.Creature, 5m, base.Creature, null);
	}

	private void EscapeMove(IReadOnlyList<Creature> targets)
	{
		if (IsHovering)
		{
			IsHovering = false;
		}
		CreatureCmd.Escape(base.Creature);
	}

	
}

