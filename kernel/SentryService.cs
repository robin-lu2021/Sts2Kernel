using System;

namespace MegaCrit.Sts2.Core;

public enum SentryLevel
{
	Debug,
	Info,
	Warning,
	Error,
	Fatal
}

public sealed class Scope
{
	public void AddCompressedAttachment(string content, string fileName)
	{
	}
}

public static class SentryService
{
	public static void CaptureException(Exception exception)
	{
	}

	public static void CaptureMessage(string message, SentryLevel level, Action<Scope>? configureScope = null)
	{
		configureScope?.Invoke(new Scope());
	}
}
