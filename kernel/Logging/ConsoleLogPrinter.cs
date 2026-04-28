using System.Diagnostics;
using System;

namespace MegaCrit.Sts2.Core.Logging;

public class ConsoleLogPrinter : ILogPrinter
{
	public void Print(LogLevel logLevel, string text, int skipFrames)
	{
		skipFrames++;
		string text2 = logLevel.ToString().ToUpperInvariant();
		switch (logLevel)
		{
		case LogLevel.Error:
		{
			StackTrace value = new StackTrace(skipFrames, fNeedFileInfo: true);
			Console.Error.WriteLine($"[{text2}] {text}\n{value}");
			break;
		}
		case LogLevel.Warn:
			Console.WriteLine("[" + text2 + "] " + text);
			break;
		default:
			Console.WriteLine("[" + text2 + "] " + text);
			break;
		}
	}
}
