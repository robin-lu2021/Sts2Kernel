using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline.Epochs;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models;

public abstract class ActModel : AbstractModel
{
	protected RoomSet _rooms;

	private IEnumerable<EncounterModel>? _allEncounters;

	private IEnumerable<EncounterModel>? _allWeakEncounters;

	private IEnumerable<EncounterModel>? _allRegularEncounters;

	private IEnumerable<EncounterModel>? _allEliteEncounters;

	private IEnumerable<EncounterModel>? _allBossEncounters;

	private IEnumerable<MonsterModel>? _allMonsters;

	private List<AncientEventModel>? _sharedAncientSubset;

	private ActModel _canonicalInstance;

	public LocString Title => new LocString("acts", base.Id.Entry + ".title");

	protected string FilePathIdentifier => base.Id.Entry.ToLowerInvariant();

	public IEnumerable<string> AssetPaths
	{
		get
		{
			List<string> obj = new List<string> { BackgroundScenePath, MapBotBgPath, MapMidBgPath, MapTopBgPath }
				.Where((string path) => !string.IsNullOrWhiteSpace(path))
				.ToList();
			IEnumerable<string> collection;
			if (!_rooms.HasAncient)
			{
				IEnumerable<string> enumerable = Array.Empty<string>();
				collection = enumerable;
			}
			else
			{
				collection = _rooms.Ancient.MapNodeAssetPaths;
			}
			obj.AddRange(collection);
			obj.AddRange(_rooms.Boss.MapNodeAssetPaths);
			IEnumerable<string> collection2;
			if (!_rooms.HasSecondBoss)
			{
				IEnumerable<string> enumerable = Array.Empty<string>();
				collection2 = enumerable;
			}
			else
			{
				collection2 = _rooms.SecondBoss.MapNodeAssetPaths;
			}
			obj.AddRange(collection2);
			return new _003C_003Ez__ReadOnlyList<string>(obj);
		}
	}

	protected virtual int NumberOfWeakEncounters => 3;

	protected abstract int BaseNumberOfRooms { get; }

	public IEnumerable<EncounterModel> AllEncounters => _allEncounters ?? (_allEncounters = GenerateAllEncounters());

	public IEnumerable<EncounterModel> AllWeakEncounters => _allWeakEncounters ?? (_allWeakEncounters = AllEncounters.Where((EncounterModel e) => e != null && e.RoomType == RoomType.Monster && e.IsWeak));

	public IEnumerable<EncounterModel> AllRegularEncounters => _allRegularEncounters ?? (_allRegularEncounters = AllEncounters.Where((EncounterModel e) => e != null && e.RoomType == RoomType.Monster && !e.IsWeak));

	public IEnumerable<EncounterModel> AllEliteEncounters => _allEliteEncounters ?? (_allEliteEncounters = AllEncounters.Where((EncounterModel e) => e.RoomType == RoomType.Elite));

	public IEnumerable<EncounterModel> AllBossEncounters => _allBossEncounters ?? (_allBossEncounters = AllEncounters.Where((EncounterModel e) => e.RoomType == RoomType.Boss));

	public IEnumerable<MonsterModel> AllMonsters => _allMonsters ?? (_allMonsters = AllEncounters.SelectMany((EncounterModel e) => e.AllPossibleMonsters).Distinct());

	public Achievement DefeatedAllEnemiesAchievement => Enum.Parse<Achievement>("Defeat" + base.Id.Entry.Capitalize() + "Enemies");

	public abstract string ChestSpineSkinNameNormal { get; }

	public abstract string ChestSpineSkinNameStroke { get; }

	public abstract IEnumerable<EncounterModel> BossDiscoveryOrder { get; }

	public abstract IEnumerable<AncientEventModel> AllAncients { get; }

	public abstract IEnumerable<EventModel> AllEvents { get; }

	public ActModel CanonicalInstance
	{
		get
		{
			if (!base.IsMutable)
			{
				return this;
			}
			return _canonicalInstance;
		}
		private set
		{
			AssertMutable();
			_canonicalInstance = value;
		}
	}

	public ActModel SourceAct => CanonicalInstance;

	public RoomSet Rooms => _rooms;

	public override bool ShouldReceiveCombatHooks => false;

	public EncounterModel BossEncounter => _rooms.Boss;

	public EncounterModel? SecondBossEncounter => _rooms.SecondBoss;

	public bool HasSecondBoss => _rooms.HasSecondBoss;

	public AncientEventModel Ancient => _rooms.Ancient;

	public string BackgroundScenePath => SceneHelper.GetScenePath($"backgrounds/{FilePathIdentifier}/{FilePathIdentifier}_background");

	public virtual string MapBotBgPath => string.Empty;

	public virtual string MapMidBgPath => string.Empty;

	public virtual string MapTopBgPath => string.Empty;

	public int GetNumberOfRooms(bool isMultiplayer)
	{
		int num = BaseNumberOfRooms;
		if (isMultiplayer)
		{
			num--;
		}
		return num;
	}

	public int GetNumberOfFloors(bool isMultiplayer)
	{
		return GetNumberOfRooms(isMultiplayer) + 2;
	}

	public abstract IEnumerable<EncounterModel> GenerateAllEncounters();

	protected override void DeepCloneFields()
	{
		_rooms = new RoomSet();
	}

	public abstract IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState state);

	public void SetSharedAncientSubset(List<AncientEventModel> sharedAncientSubset)
	{
		AssertMutable();
		_sharedAncientSubset = new List<AncientEventModel>();
		_sharedAncientSubset.AddRange(sharedAncientSubset);
	}

	public void GenerateRooms(Rng rng, UnlockState unlockState, bool isMultiplayer = false)
	{
		AssertMutable();
		List<EventModel> list = AllEvents.Concat(ModelDb.AllSharedEvents.OfType<EventModel>()).ToList();
		if (!unlockState.IsEpochRevealed<Event1Epoch>())
		{
			list.RemoveAll((EventModel e) => Event1Epoch.Events.Any((EventModel ev) => ev.Id == e.Id));
		}
		if (!unlockState.IsEpochRevealed<Event2Epoch>())
		{
			list.RemoveAll((EventModel e) => Event2Epoch.Events.Any((EventModel ev) => ev.Id == e.Id));
		}
		if (!unlockState.IsEpochRevealed<Event3Epoch>())
		{
			list.RemoveAll((EventModel e) => Event3Epoch.Events.Any((EventModel ev) => ev.Id == e.Id));
		}
		_rooms.events.AddRange(list.UnstableShuffle(rng));
		GrabBag<EncounterModel> grabBag = new GrabBag<EncounterModel>();
		for (int num = 0; num < NumberOfWeakEncounters; num++)
		{
			if (!grabBag.Any())
			{
				foreach (EncounterModel allWeakEncounter in AllWeakEncounters)
				{
					grabBag.Add(allWeakEncounter, 1.0);
				}
			}
			AddWithoutRepeatingTags(_rooms.normalEncounters, grabBag, rng);
		}
		GrabBag<EncounterModel> grabBag2 = new GrabBag<EncounterModel>();
		for (int num2 = NumberOfWeakEncounters; num2 < GetNumberOfRooms(isMultiplayer); num2++)
		{
			if (!grabBag2.Any())
			{
				foreach (EncounterModel allRegularEncounter in AllRegularEncounters)
				{
					grabBag2.Add(allRegularEncounter, 1.0);
				}
			}
			AddWithoutRepeatingTags(_rooms.normalEncounters, grabBag2, rng);
		}
		GrabBag<EncounterModel> grabBag3 = new GrabBag<EncounterModel>();
		for (int num3 = 0; num3 < 15; num3++)
		{
			if (!grabBag3.Any())
			{
				foreach (EncounterModel allEliteEncounter in AllEliteEncounters)
				{
					grabBag3.Add(allEliteEncounter, 1.0);
				}
			}
			AddWithoutRepeatingTags(_rooms.eliteEncounters, grabBag3, rng);
		}
		_rooms.Boss = rng.NextItem(AllBossEncounters);
		_rooms.Ancient = rng.NextItem(GetUnlockedAncients(unlockState).Concat(_sharedAncientSubset ?? new List<AncientEventModel>()));
	}

	public void ValidateRoomsAfterLoad(Rng rng)
	{
		if (_rooms.Boss is DeprecatedEncounter)
		{
			_rooms.Boss = rng.NextItem(AllBossEncounters);
		}
		if (_rooms.SecondBoss is DeprecatedEncounter)
		{
			EncounterModel secondBoss = rng.NextItem(AllBossEncounters.Where((EncounterModel e) => e.Id != _rooms.Boss.Id));
			_rooms.SecondBoss = secondBoss;
		}
	}

	public void ApplyDiscoveryOrderModifications(UnlockState unlockState)
	{
		foreach (EncounterModel item in BossDiscoveryOrder)
		{
			if (!unlockState.HasSeenEncounter(item))
			{
				_rooms.Boss = item;
				break;
			}
		}
		ApplyActDiscoveryOrderModifications(unlockState);
	}

	protected abstract void ApplyActDiscoveryOrderModifications(UnlockState unlockState);

	private static void AddWithoutRepeatingTags(ICollection<EncounterModel> encounters, GrabBag<EncounterModel> grabBag, Rng rng)
	{
		EncounterModel encounterModel = grabBag.GrabAndRemove(rng, (EncounterModel e) => !e.SharesTagsWith(encounters.LastOrDefault()) && e != encounters.LastOrDefault());
		if (encounterModel == null)
		{
			encounterModel = grabBag.GrabAndRemove(rng);
		}
		if (encounterModel != null)
		{
			encounters.Add(encounterModel);
		}
	}

	public EventModel PullAncient()
	{
		return _rooms.Ancient;
	}

	public EventModel PullNextEvent(RunState runState)
	{
		_rooms.EnsureNextEventIsValid(runState);
		EventModel eventModel = Hook.ModifyNextEvent(runState, _rooms.NextEvent);
		runState.AddVisitedEvent(eventModel);
		return eventModel;
	}

	public EncounterModel PullNextEncounter(RoomType roomType)
	{
		return roomType switch
		{
			RoomType.Monster => _rooms.NextNormalEncounter, 
			RoomType.Elite => _rooms.NextEliteEncounter, 
			RoomType.Boss => _rooms.NextBossEncounter, 
			_ => throw new ArgumentOutOfRangeException("roomType", roomType, null), 
		};
	}

	public void MarkRoomVisited(RoomType roomType)
	{
		_rooms.MarkVisited(roomType);
	}

	public BackgroundAssets GenerateBackgroundAssets(Rng rng)
	{
		return new BackgroundAssets(FilePathIdentifier, rng);
	}

	public void SetBossEncounter(EncounterModel encounter)
	{
		AssertMutable();
		if (encounter.RoomType != RoomType.Boss)
		{
			throw new ArgumentException("The encounter must be a boss.");
		}
		_rooms.Boss = encounter;
	}

	public void SetSecondBossEncounter(EncounterModel? encounter)
	{
		AssertMutable();
		if (encounter != null && encounter.RoomType != RoomType.Boss)
		{
			throw new ArgumentException("The encounter must be a boss.");
		}
		_rooms.SecondBoss = encounter;
	}

	public void RemoveEventFromSet(EventModel eventModel)
	{
		eventModel.AssertCanonical();
		_rooms.events.Remove(eventModel);
	}

	public ActModel ToMutable()
	{
		AssertCanonical();
		ActModel actModel = (ActModel)MutableClone();
		actModel.CanonicalInstance = this;
		return actModel;
	}

	public SerializableActModel ToSave()
	{
		AssertMutable();
		return new SerializableActModel
		{
			Id = base.Id,
			SerializableRooms = _rooms.ToSave()
		};
	}

	public static ActModel FromSave(SerializableActModel save)
	{
		ActModel actModel = ModelDb.GetById<ActModel>(save.Id).ToMutable();
		actModel._rooms = RoomSet.FromSave(save.SerializableRooms);
		return actModel;
	}

	public abstract MapPointTypeCounts GetMapPointTypes(Rng mapRng);

	public ActMap CreateMap(RunState runState, bool replaceTreasureWithElites)
	{
		return StandardActMap.CreateFor(runState, replaceTreasureWithElites);
	}

	public static IEnumerable<ActModel> GetRandomList(Rng rng, UnlockState unlockState, bool isMultiplayer)
	{
		List<ActModel> list = GetDefaultList().ToList();
		bool flag = unlockState.IsEpochRevealed<UnderdocksEpoch>();
		bool flag2 = !isMultiplayer && !SaveManager.Instance.Progress.DiscoveredActs.Contains(ModelDb.Act<Underdocks>().Id);
		if (flag && (flag2 || rng.NextBool()))
		{
			list[0] = ModelDb.Act<Underdocks>();
		}
		return list;
	}

	public static IReadOnlyList<ActModel> GetDefaultList()
	{
		return new global::_003C_003Ez__ReadOnlyArray<ActModel>(new ActModel[3]
		{
			ModelDb.Act<Overgrowth>(),
			ModelDb.Act<Hive>(),
			ModelDb.Act<Glory>()
		});
	}
}
