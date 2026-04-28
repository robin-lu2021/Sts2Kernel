using System;
using System.IO;
using System.Text;

namespace MegaCrit.Sts2.Core;

public static class Program
{
	private static Action<string>? _logWriter;
	private static Func<string?>? _reader;
	private static Action<string>? _promptWriter;
	private static Action<string>? _auditWriter;
	private static Func<string, bool>? _globalInputHandler;
	private static myBinaryPipeHandler? _pipe;

	public static bool UseSynchronousPlayerChoiceContexts { get; set; }

	public static void Log(string message)
	{
		_logWriter?.Invoke(message);
	}

	public static void Audit(string message)
	{
		_auditWriter?.Invoke(message);
	}

	public static void Write(string message)
	{
		if (_pipe != null)
		{
			_pipe.SendTextOutput(message);
		}
		else
		{
			_logWriter?.Invoke(message);
		}
	}

	public static void Prompt(string message)
	{
		if (_pipe != null)
		{
			_pipe.SendPromptInput(message);
		}
		else
		{
			_promptWriter?.Invoke(message);
		}
	}

	public static string? ReadLine()
	{
		if (_pipe != null)
		{
			return _pipe.ReadInputResponse();
		}
		return _reader?.Invoke();
	}

	public static IDisposable PushGlobalInputHandler(Func<string, bool> handler)
	{
		Func<string, bool>? previous = _globalInputHandler;
		_globalInputHandler = handler;
		return new GlobalInputHandlerScope(previous);
	}

	public static bool TryHandleGlobalInput(string input)
	{
		return _globalInputHandler?.Invoke(input) ?? false;
	}

	private sealed class GlobalInputHandlerScope : IDisposable
	{
		private readonly Func<string, bool>? _previous;

		private bool _disposed;

		public GlobalInputHandlerScope(Func<string, bool>? previous)
		{
			_previous = previous;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_globalInputHandler = _previous;
		}
	}

	public static int Main(string[] args)
	{
		Console.InputEncoding = Encoding.UTF8;
		Console.OutputEncoding = Encoding.UTF8;

		string logDir = Path.Combine(AppContext.BaseDirectory, "log");
		Directory.CreateDirectory(logDir);
		string logFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
		string logPath = Path.Combine(logDir, logFileName);

		using StreamWriter logWriter = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8)
		{
			AutoFlush = true
		};

		Action<string> writer = msg =>
		{
			Console.WriteLine(msg);
			logWriter.WriteLine(msg);
		};

		Action<string> promptWriter = msg =>
		{
			Console.Write(msg);
			logWriter.Write(msg);
		};

		Func<string?> reader = () =>
		{
			string? input = Console.ReadLine();
			if (input != null)
			{
				logWriter.WriteLine(input);
			}
			return input;
		};
		Action<string> auditWriter = msg => logWriter.WriteLine(msg);

		writer($"[LOG] Session started: {logPath}");
		_logWriter = writer;
		_auditWriter = auditWriter;
		_reader = reader;
		_promptWriter = promptWriter;
		UseSynchronousPlayerChoiceContexts = true;

		string? seed = null;
		string channelStr = "CommandLine";
		string pipeName = "sts2kernel";
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == "--seed")
			{
				seed = args[i + 1];
			}
			else if (args[i] == "--channel")
			{
				channelStr = args[i + 1];
			}
			else if (args[i] == "--pipe-name")
			{
				pipeName = args[i + 1];
			}
		}

		myGame.InteractionChannel channel = channelStr.Equals("BinaryPipe", StringComparison.OrdinalIgnoreCase)
			? myGame.InteractionChannel.BinaryPipe
			: myGame.InteractionChannel.CommandLine;

		myBinaryPipeHandler? pipe = null;
		if (channel == myGame.InteractionChannel.BinaryPipe)
		{
			writer($"[LOG] Starting BinaryPipe channel, pipe name: {pipeName}");
			pipe = new myBinaryPipeHandler(pipeName);
			writer("[LOG] Waiting for client connection...");
			pipe.WaitForConnection();
			writer("[LOG] Client connected.");
			_pipe = pipe;
		}

		myGame game = new myGame(writer: writer, reader: reader, promptWriter: promptWriter, auditWriter: auditWriter, pipe: pipe);
		try
		{
			game.RunInteractiveLoop(seed: seed, channel: channel);
			if (pipe != null)
			{
				pipe.SendExit();
			}
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex);
			logWriter.WriteLine(ex);
			return 1;
		}
		finally
		{
			pipe?.Dispose();
		}
	}
}
