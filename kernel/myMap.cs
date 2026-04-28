using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Timeline.Epochs;
using MegaCrit.Sts2.Core.Unlocks;
using KernelAncientEventModel = MegaCrit.Sts2.Core.AncientEventModel;
using KernelEventModel = MegaCrit.Sts2.Core.EventModel;

namespace MegaCrit.Sts2.Core;

/*
This file contains a kernel-oriented map subsystem.

The goal is not to copy the original src/Core map classes verbatim.
Instead, this file rebuilds the same gameplay-facing logic around map
generation, unknown-node rolling, room-pool sequencing, lightweight room
objects, hook points, and save data, while intentionally removing GUI,
asset loading, screen transitions, audio, and Godot-node concerns.

The implementation keeps the original design split:
1. upfront room-pool generation per act
2. deterministic map topology generation per act
3. runtime resolution from map-point type to concrete room type
*/

public interface IMyMapHookListener
{
	ActMap ModifyGeneratedMap(RunState runState, ActMap map, int actIndex);

	ActMap ModifyGeneratedMapLate(RunState runState, ActMap map, int actIndex);

	void AfterMapGenerated(ActMap map, int actIndex);

	IReadOnlySet<RoomType> ModifyUnknownMapPointRoomTypes(IReadOnlySet<RoomType> roomTypes);

	float ModifyOddsIncreaseForUnrolledRoomType(RoomType roomType, float oddsIncrease);
}

public sealed class myExtraRunFields
{
	public bool StartedWithNeow { get; set; }

	public int TestSubjectKills { get; set; }

	public bool FreedRepy { get; set; }
}

public struct myRunLocation(MapCoord? coord, int actIndex) : IEquatable<myRunLocation>, IComparable<myRunLocation>
{
	public int actIndex = actIndex;

	public MapCoord? coord = coord;

	public static bool operator ==(myRunLocation first, myRunLocation second)
	{
		return first.Equals(second);
	}

	public static bool operator !=(myRunLocation first, myRunLocation second)
	{
		return !first.Equals(second);
	}

	public bool Equals(myRunLocation other)
	{
		if (actIndex != other.actIndex)
		{
			return false;
		}
		if (coord.HasValue != other.coord.HasValue)
		{
			return false;
		}
		if (!coord.HasValue)
		{
			return true;
		}
		return coord.Value.Equals(other.coord!.Value);
	}

	public override bool Equals(object? obj)
	{
		return obj is myRunLocation other && Equals(other);
	}

	public override int GetHashCode()
	{
		return (actIndex, coord?.col, coord?.row).GetHashCode();
	}

	public int CompareTo(myRunLocation other)
	{
		if (actIndex != other.actIndex)
		{
			return actIndex.CompareTo(other.actIndex);
		}
		if (!coord.HasValue && !other.coord.HasValue)
		{
			return 0;
		}
		if (!coord.HasValue)
		{
			return -1;
		}
		if (!other.coord.HasValue)
		{
			return 1;
		}
		return coord.Value.CompareTo(other.coord.Value);
	}

	public override string ToString()
	{
		return $"act {actIndex} coord ({(coord.HasValue ? $"{coord.Value.col}, {coord.Value.row}" : "null")})";
	}
}

public sealed class myRoomSet
{
	public readonly List<KernelEventModel> events = new List<KernelEventModel>();

	public int eventsVisited;

	public readonly List<EncounterModel> normalEncounters = new List<EncounterModel>();

	public int normalEncountersVisited;

	public readonly List<EncounterModel> eliteEncounters = new List<EncounterModel>();

	public int eliteEncountersVisited;

	public int bossEncountersVisited;

	public KernelAncientEventModel Ancient { get; set; } = null!;

	public EncounterModel Boss { get; set; } = null!;

	public EncounterModel? SecondBoss { get; set; }

	public bool HasSecondBoss => SecondBoss != null;

	public KernelEventModel NextEvent => events[eventsVisited % events.Count];

	public EncounterModel NextNormalEncounter => normalEncounters[normalEncountersVisited % normalEncounters.Count];

	public EncounterModel NextEliteEncounter => eliteEncounters[eliteEncountersVisited % eliteEncounters.Count];

	public EncounterModel NextBossEncounter
	{
		get
		{
			if (bossEncountersVisited != 0 && SecondBoss != null)
			{
				return SecondBoss;
			}
			return Boss;
		}
	}

	public void MarkVisited(RoomType roomType)
	{
		switch (roomType)
		{
		case RoomType.Monster:
			normalEncountersVisited++;
			break;
		case RoomType.Elite:
			eliteEncountersVisited++;
			break;
		case RoomType.Event:
			eventsVisited++;
			break;
		case RoomType.Boss:
			bossEncountersVisited++;
			break;
		}
	}

	public void EnsureNextEventIsValid(RunState runState)
	{
		if (events.Count == 0)
		{
			return;
		}
		RunState? sourceRunState = runState.SourceRunState;
		for (int i = 0; i < events.Count; i++)
		{
			bool isAllowed = sourceRunState == null || NextEvent.IsAllowed(sourceRunState);
			if (isAllowed && !runState.VisitedEventIds.Any((ModelId visitedEventId) => visitedEventId == NextEvent.ModelId))
			{
				return;
			}
			eventsVisited++;
		}
	}

	public static void SwapToOrCreateAtIndex<TBaseModel, TSpecificModel>(List<TBaseModel> list, int desiredIndex) where TBaseModel : AbstractModel where TSpecificModel : TBaseModel
	{
		while (list.Count <= desiredIndex)
		{
			list.Add(ModelDb.GetById<TBaseModel>(ModelDb.GetId<TSpecificModel>()));
		}
		int existingIndex = list.FindIndex((TBaseModel item) => item is TSpecificModel);
		if (existingIndex >= 0)
		{
			TBaseModel temp = list[desiredIndex];
			list[desiredIndex] = list[existingIndex];
			list[existingIndex] = temp;
		}
		else
		{
			list[desiredIndex] = ModelDb.GetById<TBaseModel>(ModelDb.GetId<TSpecificModel>());
		}
	}
}

public sealed class myRunManager
{
	public RunState State { get; }

	public Dictionary<int, SerializableActMap>? SavedMapsToLoad { get; set; }

	public myRunManager(RunState state)
	{
		State = state ?? throw new ArgumentNullException(nameof(state));
	}

	public void GenerateRooms()
	{
		List<KernelAncientEventModel> sharedAncients = State.UnlockState.SharedAncients.ToList().UnstableShuffle(State.Rng.UpFront);
		foreach (ActModel act in State.Acts.Skip(1))
		{
			int takeCount = State.Rng.UpFront.NextInt(sharedAncients.Count + 1);
			List<KernelAncientEventModel> subset = sharedAncients.Take(takeCount).ToList();
			sharedAncients = sharedAncients.Except(subset).ToList();
			act.SetSharedAncientSubset(subset);
		}
		for (int i = 0; i < State.Acts.Count; i++)
		{
			ActModel act = State.Acts[i];
			act.GenerateRooms(State.Rng.UpFront, State.UnlockState, State.PlayerCount > 1);
			if (i == State.Acts.Count - 1 && State.AscensionLevel >= (int)AscensionLevel.DoubleBoss)
			{
				EncounterModel? secondBoss = State.Rng.UpFront.NextItem(act.AllBossEncounters.Where((EncounterModel encounter) => encounter.Id != act.Rooms.Boss.Id));
				if (secondBoss != null)
				{
					act.SetSecondBossEncounter(secondBoss);
				}
			}
		}
	}

	public void GenerateMap(bool replaceTreasureWithElites = false)
	{
		using IDisposable ascensionOverride = AscensionHelper.PushOverride(State.AscensionLevel);
		ActMap map;
		if (SavedMapsToLoad != null && SavedMapsToLoad.TryGetValue(State.CurrentActIndex, out SerializableActMap? savedMap))
		{
			map = new SavedActMap(savedMap);
			SavedMapsToLoad.Remove(State.CurrentActIndex);
			if (SavedMapsToLoad.Count == 0)
			{
				SavedMapsToLoad = null;
			}
			map = ModifyGeneratedMapLate(map, State.CurrentActIndex);
			AfterMapGenerated(map, State.CurrentActIndex);
		}
		else
		{
			ActMap generated = State.Act.CreateMap(State, replaceTreasureWithElites);
			map = ModifyGeneratedMap(generated, State.CurrentActIndex);
			AfterMapGenerated(map, State.CurrentActIndex);
			if (!State.ExtraFields.StartedWithNeow && State.CurrentActIndex == 0)
			{
				map.StartingMapPoint.PointType = MapPointType.Monster;
			}
		}
		State.Map = map;
	}

	public static HashSet<RoomType> BuildRoomTypeBlacklist(MapPointHistoryEntry? previousMapPointEntry, IReadOnlyCollection<MapPoint> nextMapPoints)
	{
		HashSet<RoomType> result = new HashSet<RoomType>();
		if ((previousMapPointEntry != null && previousMapPointEntry.HasRoomOfType(RoomType.Shop) || (nextMapPoints.Count > 0 && nextMapPoints.All((MapPoint point) => point.PointType == MapPointType.Shop))))
		{
			result.Add(RoomType.Shop);
		}
		return result;
	}

	private ActMap ModifyGeneratedMap(ActMap map, int actIndex)
	{
		foreach (IMyMapHookListener listener in State.HookListeners)
		{
			map = listener.ModifyGeneratedMap(State, map, actIndex);
		}
		return ModifyGeneratedMapLate(map, actIndex);
	}

	private ActMap ModifyGeneratedMapLate(ActMap map, int actIndex)
	{
		foreach (IMyMapHookListener listener in State.HookListeners)
		{
			map = listener.ModifyGeneratedMapLate(State, map, actIndex);
		}
		return map;
	}

	private void AfterMapGenerated(ActMap map, int actIndex)
	{
		foreach (IMyMapHookListener listener in State.HookListeners)
		{
			listener.AfterMapGenerated(map, actIndex);
		}
	}
}
