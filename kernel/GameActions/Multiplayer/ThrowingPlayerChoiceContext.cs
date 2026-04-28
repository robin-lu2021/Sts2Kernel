using System;
using MegaCrit.Sts2.Core.Entities.Multiplayer;

namespace MegaCrit.Sts2.Core.GameActions.Multiplayer;

public class ThrowingPlayerChoiceContext : PlayerChoiceContext
{
	public override void SignalPlayerChoiceBegun(PlayerChoiceOptions options)
	{
		throw new NotImplementedException();
	}

	public override void SignalPlayerChoiceEnded()
	{
		throw new NotImplementedException();
	}
}
