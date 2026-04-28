using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Timeline;

public sealed class EpochComparer : IComparer<SerializableEpoch>
{
	public int Compare(SerializableEpoch? x, SerializableEpoch? y)
	{
		if (ReferenceEquals(x, y))
		{
			return 0;
		}
		if (x == null)
		{
			return -1;
		}
		if (y == null)
		{
			return 1;
		}
		int stateComparison = y.State.CompareTo(x.State);
		if (stateComparison != 0)
		{
			return stateComparison;
		}
		int dateComparison = y.ObtainDate.CompareTo(x.ObtainDate);
		if (dateComparison != 0)
		{
			return dateComparison;
		}
		return string.CompareOrdinal(x.Id, y.Id);
	}
}
