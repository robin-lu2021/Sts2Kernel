using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Logging;

public class Logger
{
	private static readonly object _lockObj = new object();

	public static LogLevel GlobalLogLevel { get; set; } = LogLevel.Info;

	private static readonly ILogPrinter _logPrinter = new ConsoleLogPrinter();

	private readonly LogType _logType;

	public static readonly Dictionary<LogType, LogLevel> logLevelTypeMap = new Dictionary<LogType, LogLevel>
	{
		{
			LogType.Network,
			LogLevel.Info
		},
		{
			LogType.Actions,
			LogLevel.Info
		},
		{
			LogType.Generic,
			LogLevel.Info
		},
		{
			LogType.GameSync,
			LogLevel.Info
		}
	};

	public string? Context { get; set; }

	public event Action<LogLevel, string, int>? LogCallback;

	private static bool GetIsRunningFromGodotEditor()
	{
		string[] cmdlineArgs = Environment.GetCommandLineArgs();
		bool flag = cmdlineArgs.Any((string arg) => arg == "--headless");
		bool flag2 = cmdlineArgs.Any((string arg) => arg.Contains("CiCoreRunner.tscn"));
		return !flag && !flag2 && TestMode.IsOff && cmdlineArgs.Any((string arg) => arg == "--editor");
	}

	static Logger()
	{
		string[] commandLineArgs = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (!(commandLineArgs[i] != "-log"))
			{
				if (!Enum.TryParse(commandLineArgs[i + 1], ignoreCase: true, out LogType enumVal))
				{
					_logPrinter.Print(LogLevel.Error, "Invalid log command line argument! Could not parse " + commandLineArgs[i + 1] + " as LogType", 1);
					continue;
				}
				if (!Enum.TryParse(commandLineArgs[i + 2], ignoreCase: true, out LogLevel enumVal2))
				{
					_logPrinter.Print(LogLevel.Error, "Invalid log command line argument! Could not parse " + commandLineArgs[i + 2] + " as LogLevel", 1);
					continue;
				}
				logLevelTypeMap[enumVal] = enumVal2;
				_logPrinter.Print(LogLevel.Info, $"Log level for {enumVal} set to {enumVal2}", 1);
			}
		}
	}

	public Logger(string? context, LogType logType)
	{
		Context = context;
		_logType = logType;
		LogCallback += Log.InvokeGlobalLogCallback;
	}

	public bool WillLog(LogLevel level)
	{
		LogLevel value;
		return level >= ((logLevelTypeMap.TryGetValue(_logType, out value) ? new LogLevel?(value) : ((LogLevel?)null)) ?? GlobalLogLevel);
	}

	public void LogMessage(LogLevel level, string text, int skipFrames)
	{
		skipFrames++;
		string text2 = ((Context != null) ? ("[" + Context + "] " + text) : text);
		LogMessage(level, _logType, text2, skipFrames);
	}

	public void LogMessage(LogLevel level, LogType type, string text, int skipFrames)
	{
		skipFrames++;
		if (!WillLog(level))
		{
			return;
		}
		lock (_lockObj)
		{
			_logPrinter.Print(level, text, skipFrames);
			this.LogCallback?.Invoke(level, text, skipFrames);
		}
	}

	public void Load(string text, int skipFrames = 1)
	{
		LogMessage(LogLevel.Load, text, skipFrames);
	}

	public void Debug(string text, int skipFrames = 1)
	{
		LogMessage(LogLevel.Debug, text, skipFrames);
	}

	public void VeryDebug(string text, int skipFrames = 1)
	{
		LogMessage(LogLevel.VeryDebug, text, skipFrames);
	}

	public void Info(string text, int skipFrames = 1)
	{
		LogMessage(LogLevel.Info, text, skipFrames);
	}

	public void Warn(string text, int skipFrames = 1)
	{
		LogMessage(LogLevel.Warn, text, skipFrames);
	}

	public void Error(string text, int skipFrames = 1)
	{
		LogMessage(LogLevel.Error, text, skipFrames);
	}

	public static void SetLogLevelForType(LogType type, LogLevel? logLevel)
	{
		if (logLevel.HasValue)
		{
			logLevelTypeMap[type] = logLevel.Value;
		}
		else
		{
			logLevelTypeMap.Remove(type);
		}
	}
}
