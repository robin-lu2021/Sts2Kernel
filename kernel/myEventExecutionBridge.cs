using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using KernelEventModel = MegaCrit.Sts2.Core.EventModel;

namespace MegaCrit.Sts2.Core;

/*
This bridge keeps a kernel-owned event room alive while executing legacy event
logic directly against the current run's real Player/RunState.

It deliberately does not try to reintroduce the original room manager or GUI.
Instead it:
1. runs Begin/Choose/Resume against the existing event model
2. keeps event state on the live run/player objects
3. exposes a combat-request handoff so myGame can own the room stack and state machine
*/

public sealed class myEventExecutionBridge
{
	private readonly KernelEventModel _event;

	public EventRoom Room { get; }

	public Player Player { get; }

	public RunState RunState { get; }

	public KernelEventModel Event => _event;

	public bool HasStarted { get; private set; }

	public bool IsSuspendedForCombat { get; private set; }

	public bool IsFinished => Event.IsFinished;

	public myEventExecutionBridge(EventRoom room, Player player, RunState runState)
	{
		Room = room ?? throw new ArgumentNullException(nameof(room));
		_event = room.CanonicalEvent.ToMutable();
		Player = player ?? throw new ArgumentNullException(nameof(player));
		RunState = runState ?? throw new ArgumentNullException(nameof(runState));
	}

	public void Begin()
	{
		if (HasStarted)
		{
			return;
		}
		ExecuteHeadless(() =>
		{
			Event.BeginEvent(Player, Room.IsPreFinished);
			Event.OnRoomEnter();
			Event.AfterEventStarted();
		});
		HasStarted = true;
	}

	public myEventCombatRequest? ChooseOption(int optionIndex)
	{
		if (!HasStarted)
		{
			throw new InvalidOperationException("Event has not been started yet.");
		}
		if (IsSuspendedForCombat)
		{
			throw new InvalidOperationException("Event is currently suspended for combat.");
		}
		ExecuteHeadless(() => Event.ChooseOption(optionIndex));
		return TakePendingCombatRequest();
	}

	public myEventCombatRequest? TakePendingCombatRequest()
	{
		myEventCombatRequest? request = Event.TakePendingCombatRequest();
		if (request != null)
		{
			IsSuspendedForCombat = true;
		}
		return request;
	}

	public void SuspendForCombat()
	{
		IsSuspendedForCombat = true;
	}

	public void ResumeAfterCombat(myCombatSession session)
	{
		if (session == null)
		{
			throw new ArgumentNullException(nameof(session));
		}
		if (!ReferenceEquals(session.ParentEventBridge, this))
		{
			throw new InvalidOperationException("Combat session does not belong to this event bridge.");
		}
		IsSuspendedForCombat = false;
		CombatRoom exitedRoom = session.CreateExitedCoreRoom(RunState);
		ExecuteHeadless(() =>
		{
			Event.Resume(exitedRoom);
			Event.OnRoomEnter();
		});
	}

	public void EnsureCleanup()
	{
		ExecuteHeadless(Event.EnsureCleanup);
	}

	public void AppendCombatRoomToHistory(CombatRoom room)
	{
		if (room == null)
		{
			throw new ArgumentNullException(nameof(room));
		}
		MapPointHistoryEntry? currentEntry = RunState.CurrentMapPointHistoryEntry;
		if (currentEntry == null)
		{
			return;
		}
		currentEntry.Rooms.Add(new MapPointRoomHistoryEntry
		{
			RoomType = room.RoomType,
			ModelId = room.ModelId
		});
	}

	public IReadOnlyList<string> GetOptionSummaries()
	{
		List<string> options = new List<string>();
		IReadOnlyList<myEventOption> currentOptions = Event.CurrentOptions;
		for (int i = 0; i < currentOptions.Count; i++)
		{
			myEventOption option = currentOptions[i];
			StringBuilder builder = new StringBuilder();
			builder.Append(i + 1).Append(". ");
			builder.Append(SafeFormat(option.Title, option.TextKey));
			if (option.IsLocked)
			{
				builder.Append(" [locked]");
			}
			if (option.WasChosen)
			{
				builder.Append(" [chosen]");
			}
			string description = SafeFormat(option.Description, string.Empty);
			if (!string.IsNullOrWhiteSpace(description))
			{
				builder.Append(" - ").Append(description);
			}
			options.Add(builder.ToString());
		}
		return options;
	}

	public string BuildSummary()
	{
		StringBuilder builder = new StringBuilder();
		builder.Append("Event room");
		builder.Append(" | Event=").Append(Event.Id);
		builder.Append(" | Finished=").Append(Event.IsFinished);
		builder.Append(" | SuspendedForCombat=").Append(IsSuspendedForCombat);
		builder.AppendLine();
		builder.Append("Title: ").Append(SafeFormat(Event.Title, Event.Id));
		builder.AppendLine();
		builder.Append("Description: ").Append(SafeFormat(Event.Description ?? Event.InitialDescription, "(unavailable)"));
		IReadOnlyList<string> options = GetOptionSummaries();
		if (options.Count > 0)
		{
			builder.AppendLine();
			builder.Append("Options:");
			foreach (string option in options)
			{
				builder.AppendLine();
				builder.Append(option);
			}
		}
		return builder.ToString();
	}

	private static void ExecuteHeadless(Action action)
	{
		ulong? previousLocalNetId = LocalContext.NetId;
		LocalContext.NetId = null;
		try
		{
			action();
		}
		finally
		{
			LocalContext.NetId = previousLocalNetId;
		}
	}

	private static string SafeFormat(LocString? locString, string fallback)
	{
		if (locString == null)
		{
			return fallback;
		}
		try
		{
			return locString.GetFormattedText();
		}
		catch
		{
			return fallback;
		}
	}
}
