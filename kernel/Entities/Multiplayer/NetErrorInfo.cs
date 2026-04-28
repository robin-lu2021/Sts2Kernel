using System;
using System.Collections.Generic;
using System.Text;
using MegaCrit.Sts2.Core.Localization;

namespace MegaCrit.Sts2.Core.Entities.Multiplayer;

public readonly struct NetErrorInfo
{
	private readonly NetError? _reason;

	private readonly ConnectionFailureReason? _connectionReason;

	private readonly ConnectionFailureExtraInfo? _connectionExtraInfo;

	public bool SelfInitiated { get; }

	public NetErrorInfo(NetError reason, bool selfInitiated)
	{
		_connectionReason = null;
		_connectionExtraInfo = null;
		_reason = reason;
		SelfInitiated = selfInitiated;
	}

	public NetErrorInfo(ConnectionFailureReason reason, ConnectionFailureExtraInfo? extraInfo = null)
	{
		_reason = null;
		_connectionReason = reason;
		_connectionExtraInfo = extraInfo;
		SelfInitiated = false;
	}

	public NetError GetReason()
	{
		if (_reason.HasValue)
		{
			return _reason.Value;
		}
		if (_connectionReason.HasValue)
		{
			ConnectionFailureReason value = _connectionReason.Value;
			switch (value)
			{
			case ConnectionFailureReason.None:
				return NetError.None;
			case ConnectionFailureReason.LobbyFull:
				return NetError.LobbyFull;
			case ConnectionFailureReason.RunInProgress:
				return NetError.RunInProgress;
			case ConnectionFailureReason.NotInSaveGame:
				return NetError.NotInSaveGame;
			case ConnectionFailureReason.VersionMismatch:
				return NetError.VersionMismatch;
			case ConnectionFailureReason.ModMismatch:
				return NetError.ModMismatch;
			default:
			{
				throw new System.Runtime.CompilerServices.SwitchExpressionException(value);
				NetError result = default(NetError);
				return result;
				}
			}
		}
		throw new InvalidOperationException("Tried to get DisconnectionReason from DisconnectionInfo without any assigned errors");
	}

	public string GetErrorString()
	{
		if (_reason.HasValue)
		{
			return _reason.Value.ToString();
		}
		if (_connectionReason.HasValue)
		{
			if (_connectionReason == ConnectionFailureReason.ModMismatch)
			{
				StringBuilder stringBuilder = new StringBuilder();
				List<string> list = _connectionExtraInfo?.missingModsOnHost;
				if (list != null && list.Count > 0)
				{
					LocString locString = new LocString("main_menu_ui", "NETWORK_ERROR.MOD_MISMATCH.description.missingOnHost");
					locString.Add("mods", string.Join(", ", _connectionExtraInfo.missingModsOnHost));
					stringBuilder.AppendLine(locString.GetFormattedText());
				}
				list = _connectionExtraInfo?.missingModsOnLocal;
				if (list != null && list.Count > 0)
				{
					LocString locString2 = new LocString("main_menu_ui", "NETWORK_ERROR.MOD_MISMATCH.description.missingOnLocal");
					locString2.Add("mods", string.Join(", ", _connectionExtraInfo.missingModsOnLocal));
					stringBuilder.AppendLine(locString2.GetFormattedText());
				}
				return stringBuilder.ToString();
			}
			return _connectionReason.Value.ToString();
		}
		return "<null>";
	}

	public override string ToString()
	{
		if (_reason.HasValue)
		{
			return $"DisconnectionReason {_reason.Value} {SelfInitiated}";
		}
		if (_connectionReason.HasValue)
		{
			return $"ConnectionFailureReason {_connectionReason.Value} {SelfInitiated}";
		}
		return "<null>";
	}
}
