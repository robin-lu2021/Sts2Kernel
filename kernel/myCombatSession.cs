using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using CoreRunState = MegaCrit.Sts2.Core.Runs.RunState;

namespace MegaCrit.Sts2.Core;

public enum myCombatResult
{
	None,
	Victory,
	Defeat,
	Escape
}

public enum myCombatSessionState
{
	Created,
	Active,
	Victory,
	Defeat,
	Escape,
	Completed
}

public sealed class myCombatSession
{
	public CombatRoom Room { get; }

	public myEventExecutionBridge? ParentEventBridge { get; }

	public IReadOnlyList<Reward> ExtraRewards { get; }

	public bool ShouldResumeParentEventAfterCombat { get; }

	public myCombatSessionState State { get; private set; } = myCombatSessionState.Created;

	public myCombatResult Result { get; private set; } = myCombatResult.None;

	public bool IsFromEvent => ParentEventBridge != null;

	public bool IsResolved => Result != myCombatResult.None;

	private myCombatSession(CombatRoom room, myEventExecutionBridge? parentEventBridge, IEnumerable<Reward>? extraRewards, bool shouldResumeParentEventAfterCombat)
	{
		Room = room ?? throw new ArgumentNullException(nameof(room));
		ParentEventBridge = parentEventBridge;
		ExtraRewards = (extraRewards ?? Array.Empty<Reward>()).ToList();
		ShouldResumeParentEventAfterCombat = shouldResumeParentEventAfterCombat;
	}

	public static myCombatSession CreateForRoom(CombatRoom room)
	{
		return new myCombatSession(room, parentEventBridge: null, extraRewards: Array.Empty<Reward>(), shouldResumeParentEventAfterCombat: false);
	}

	public static myCombatSession CreateForEventRequest(CombatRoom room, myEventExecutionBridge bridge, myEventCombatRequest request)
	{
		if (bridge == null)
		{
			throw new ArgumentNullException(nameof(bridge));
		}
		if (request == null)
		{
			throw new ArgumentNullException(nameof(request));
		}
		return new myCombatSession(room, bridge, request.ExtraRewards, request.ShouldResumeAfterCombat);
	}

	public void Begin()
	{
		if (State != myCombatSessionState.Created)
		{
			return;
		}
		State = myCombatSessionState.Active;
	}

	public void ResolveVictory()
	{
		RequireActive();
		Result = myCombatResult.Victory;
		State = myCombatSessionState.Victory;
	}

	public void ResolveDefeat()
	{
		RequireActive();
		Result = myCombatResult.Defeat;
		State = myCombatSessionState.Defeat;
	}

	public void ResolveEscape()
	{
		RequireActive();
		Result = myCombatResult.Escape;
		State = myCombatSessionState.Escape;
	}

	public void MarkCompleted()
	{
		if (!IsResolved)
		{
			throw new InvalidOperationException("Combat session must be resolved before completion.");
		}
		State = myCombatSessionState.Completed;
	}

	public CombatRoom CreateExitedCoreRoom(CoreRunState? runState)
	{
		EncounterModel encounter = Room.Encounter.CanonicalInstance.ToMutable();
		encounter.LoadCustomState(Room.Encounter.SaveCustomState());
		if (encounter is BattlewornDummyEventEncounter battlewornDummyEncounter && (Result == myCombatResult.Defeat || Result == myCombatResult.Escape))
		{
			battlewornDummyEncounter.RanOutOfTime = true;
		}
		return new CombatRoom(encounter, runState)
		{
			ShouldCreateCombat = false,
			ShouldResumeParentEventAfterCombat = ShouldResumeParentEventAfterCombat,
			ParentEventId = ResolveParentEventId()
		};
	}

	public string BuildSummary()
	{
		StringBuilder builder = new StringBuilder();
		builder.Append("Combat room");
		builder.Append(" | Encounter=").Append(Room.Encounter.Id.Entry);
		builder.Append(" | Type=").Append(Room.RoomType);
		builder.Append(" | State=").Append(State);
		builder.Append(" | Result=").Append(Result);
		if (IsFromEvent)
		{
			builder.Append(" | ParentEvent=").Append(ParentEventBridge?.Event.Id);
			builder.Append(" | ResumeEvent=").Append(ShouldResumeParentEventAfterCombat);
		}
		if (ExtraRewards.Count > 0)
		{
			builder.AppendLine();
			builder.Append("Extra rewards: ").Append(string.Join(", ", ExtraRewards.Select((Reward reward) => reward.GetType().Name)));
		}
		return builder.ToString();
	}

	private void RequireActive()
	{
		if (State != myCombatSessionState.Active)
		{
			throw new InvalidOperationException($"Combat session must be active. Current state: {State}.");
		}
	}

	private ModelId? ResolveParentEventId()
	{
		if (ParentEventBridge == null)
		{
			return null;
		}
		string eventId = ParentEventBridge.Event.Id;
		return ModelDb.AllEvents
			.Concat(ModelDb.AllSharedEvents)
			.Select((EventModel candidate) => new ModelId("events", candidate.Id))
			.FirstOrDefault((ModelId id) => id.Entry.Equals(eventId, StringComparison.OrdinalIgnoreCase));
	}
}
