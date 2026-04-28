using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Rooms;

public class EventRoom : AbstractRoom
{
	private bool _isPreFinished;

	public override RoomType RoomType => RoomType.Event;

	public override ModelId ModelId => CanonicalEvent.ModelId;

	public EventModel CanonicalEvent { get; }

	public EventModel LocalMutableEvent => HasActiveSynchronizedEvents()
		? RunManager.Instance.EventSynchronizer.GetLocalEvent()
		: CanonicalEvent;

	public Action<EventModel>? OnStart { private get; init; }

	public override bool IsPreFinished => _isPreFinished;

	public EventRoom(EventModel eventModel)
	{
		eventModel.AssertCanonical();
		CanonicalEvent = eventModel;
	}

	public EventRoom(SerializableRoom serializableRoom)
	{
		CanonicalEvent = SaveUtil.EventOrDeprecated(serializableRoom.EventId);
		if (serializableRoom.IsPreFinished)
		{
			MarkPreFinished();
		}
	}

	public override void EnterInternal(IRunState? runState, bool isRestoringRoomStackBase)
	{
		RunManager.Instance.EventSynchronizer.BeginEvent(CanonicalEvent, IsPreFinished, OnStart);
		foreach (EventModel @event in RunManager.Instance.EventSynchronizer.Events)
		{
			@event.StateChanged += OnEventStateChanged;
			if (@event.IsFinished && !IsPreFinished)
			{
				OnEventStateChanged(@event);
			}
		}
		EventModel localEvent = RunManager.Instance.EventSynchronizer.GetLocalEvent();
		if (localEvent.LayoutType == EventLayoutType.Combat)
		{
			localEvent.GenerateInternalCombatState(runState ?? NullRunState.Instance);
		}
		if (runState != null)
		{
			Hook.AfterRoomEntered(runState, this);
		}
		localEvent.AfterEventStarted();
	}

	public override void Exit(IRunState? runState)
	{
		if (!HasActiveSynchronizedEvents())
		{
			return;
		}
		EventModel localEvent = RunManager.Instance.EventSynchronizer.GetLocalEvent();
		if (localEvent.IsDeterministic)
		{
			RunManager.Instance.ChecksumTracker.GenerateChecksum($"Exiting event room {localEvent.Id}", null);
		}
		if (localEvent.LayoutType == EventLayoutType.Combat)
		{
			localEvent.ResetInternalCombatState();
		}
		foreach (EventModel @event in RunManager.Instance.EventSynchronizer.Events)
		{
			@event.StateChanged -= OnEventStateChanged;
			@event.EnsureCleanup();
		}
		return;
	}

	public override void Resume(AbstractRoom exitedRoom, IRunState? runState)
	{
		if (!HasActiveSynchronizedEvents())
		{
			return;
		}
		RunManager.Instance.EventSynchronizer.ResumeEvents(exitedRoom);
		return;
	}

	public override SerializableRoom ToSerializable()
	{
		SerializableRoom serializableRoom = base.ToSerializable();
		serializableRoom.EventId = CanonicalEvent.ModelId;
		serializableRoom.IsPreFinished = IsPreFinished;
		return serializableRoom;
	}

	public void MarkPreFinished()
	{
		_isPreFinished = true;
	}

	private void OnEventStateChanged(EventModel eventModel)
	{
		if (!(eventModel is AncientEventModel))
		{
			return;
		}
		foreach (EventModel @event in RunManager.Instance.EventSynchronizer.Events)
		{
			if (!@event.IsFinished)
			{
				return;
			}
		}
		MarkPreFinished();
		SaveManager.Instance.SaveRun(this);
	}

	private static bool HasActiveSynchronizedEvents()
	{
		return RunManager.Instance.EventSynchronizer.Events.Count > 0;
	}
}
