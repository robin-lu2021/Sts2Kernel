using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class SpoilsMap : CardModel
{
	private int _spoilsActIndex = -1;

	public override int MaxUpgradeLevel => 0;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new GoldVar(600));

	public override IEnumerable<CardKeyword> CanonicalKeywords => new global::_003C_003Ez__ReadOnlySingleElementList<CardKeyword>(CardKeyword.Unplayable);

	[SavedProperty]
	public int SpoilsActIndex
	{
		get
		{
			return _spoilsActIndex;
		}
		set
		{
			AssertMutable();
			_spoilsActIndex = value;
		}
	}

	public MapCoord? SpoilsCoord { get; private set; }

	public SpoilsMap()
		: base(-1, CardType.Quest, CardRarity.Quest, TargetType.Self)
	{
	}

	public override void AfterCreated()
	{
		SpoilsActIndex = 1;
	}

	public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
	{
		if (actIndex != SpoilsActIndex)
		{
			return map;
		}
		CardPile? pile = base.Pile;
		if (pile == null || pile.Type != PileType.Deck)
		{
			return map;
		}
		return new SpoilsActMap(runState);
	}

	public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
	{
		if (actIndex != SpoilsActIndex)
		{
			return map;
		}
		CardPile? pile = base.Pile;
		if (pile == null || pile.Type != PileType.Deck)
		{
			return map;
		}
		MapPoint mapPoint = map.GetAllMapPoints().FirstOrDefault((MapPoint p) => p.PointType == MapPointType.Treasure);
		if (mapPoint != null)
		{
			SpoilsCoord = mapPoint.coord;
		}
		return map;
	}

	public override void AfterMapGenerated(ActMap map, int actIndex)
	{
		CardPile? pile = base.Pile;
		if (pile == null || pile.Type != PileType.Deck)
		{
			return;
		}
		if (actIndex != SpoilsActIndex)
		{
			return;
		}
		if (SpoilsCoord.HasValue && map.HasPoint(SpoilsCoord.Value))
		{
			map.GetPoint(SpoilsCoord.Value)?.AddQuest(this);
		}
		return;
	}

	public override void BeforeCardRemoved(CardModel card)
	{
		if (card != this)
		{
			return;
		}
		if (SpoilsActIndex != base.Owner.RunState.CurrentActIndex)
		{
			return;
		}
		if (!SpoilsCoord.HasValue)
		{
			return;
		}
		base.Owner.RunState.Map.GetPoint(SpoilsCoord.Value)?.RemoveQuest(this);
		return;
	}

	public int OnQuestComplete()
	{
		PlayerCmd.GainGold(base.DynamicVars.Gold.BaseValue, base.Owner);
		PlayerCmd.CompleteQuest(this);
		CardPileCmd.RemoveFromDeck(this);
		return base.DynamicVars.Gold.IntValue;
	}
}
