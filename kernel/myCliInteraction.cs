using System;
using System.Collections.Generic;
using System.Linq;

namespace MegaCrit.Sts2.Core;

public sealed class myCliChoice
{
	public string Key { get; init; } = string.Empty;

	public string Text { get; init; } = string.Empty;

	public int Index { get; init; }

	public object? Payload { get; init; }

	public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
}

public sealed class myCliInteraction
{
	private readonly List<myCliChoice> _currentOptions = new List<myCliChoice>();

	private readonly Action<string> _writer;

	private readonly Action<string> _promptWriter;

	private readonly Func<string?> _reader;

	private readonly Action<string>? _auditWriter;

	private myBinaryPipeHandler? _pipe;

	public myCliInteraction(Action<string> writer, Func<string?> reader, Action<string> promptWriter, Action<string>? auditWriter = null)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
		_reader = reader ?? throw new ArgumentNullException(nameof(reader));
		_promptWriter = promptWriter ?? throw new ArgumentNullException(nameof(promptWriter));
		_auditWriter = auditWriter;
	}

	/// <summary>Attaches a binary pipe handler. When set, all I/O is routed through the pipe.</summary>
	public void SetPipeHandler(myBinaryPipeHandler? pipe)
	{
		_pipe = pipe;
	}

	private bool IsPipe => _pipe != null;

	public void WriteLine(string message, myGame.InteractionChannel channel)
	{
		if (IsPipe)
		{
			_writer(message);
			_pipe!.SendTextOutput(message);
			return;
		}
		_writer(message);
	}

	public void WriteImportant(string message, myGame.InteractionChannel channel)
	{
		WriteLine($"[IMPORTANT] {message}", channel);
	}

	public string? ReadInput(string prompt, myGame.InteractionChannel channel)
	{
		if (IsPipe)
		{
			_promptWriter(prompt);
			_pipe!.SendPromptInput(prompt);
			string? input = _pipe!.ReadInputResponse();
			if (input != null)
			{
				_writer(input);
			}
			return input;
		}
		_promptWriter(prompt);
		return _reader();
	}

	public void ShowOptions(IEnumerable<string> options, myGame.InteractionChannel channel)
	{
		ShowChoices(options.Select((string option, int index) => new myCliChoice
		{
			Key = (index + 1).ToString(),
			Index = index + 1,
			Text = option
		}), channel);
	}

	public void ShowChoices(IEnumerable<myCliChoice> choices, myGame.InteractionChannel channel)
	{
		List<myCliChoice> resolvedChoices = choices?.ToList() ?? throw new ArgumentNullException(nameof(choices));
		if (resolvedChoices.Count == 0)
		{
			throw new ArgumentException("At least one option is required.", nameof(choices));
		}
		_currentOptions.Clear();
		_currentOptions.AddRange(resolvedChoices);
		if (IsPipe)
		{
			_pipe!.SendShowChoices(_currentOptions);
		}

		for (int i = 0; i < _currentOptions.Count; i++)
		{
			myCliChoice choice = _currentOptions[i];
			_writer($"{choice.Key}: {choice.Text}");
		}
		LogOptions();
	}

	public int GetOption(myGame.InteractionChannel channel, Func<string, bool?>? tryHandleGlobalCommand = null, Func<string, bool>? tryHandleExit = null)
	{
		myCliChoice? choice = GetChoice(channel, tryHandleGlobalCommand, tryHandleExit);
		if (choice == null)
		{
			throw new InvalidOperationException("Input ended before an option was selected.");
		}
		return choice.Index;
	}

	public myCliChoice? PromptChoice(string prompt, IEnumerable<myCliChoice> choices, myGame.InteractionChannel channel, Func<string, bool?>? tryHandleGlobalCommand = null, Func<string, bool>? tryHandleExit = null)
	{
		ShowChoices(choices, channel);
		return GetChoice(channel, tryHandleGlobalCommand, tryHandleExit, prompt);
	}

	public myCliChoice? GetChoice(myGame.InteractionChannel channel, Func<string, bool?>? tryHandleGlobalCommand = null, Func<string, bool>? tryHandleExit = null, string? prompt = null)
	{
		if (_currentOptions.Count == 0)
		{
			throw new InvalidOperationException("No options are available. Call ShowChoices() first.");
		}
		string resolvedPrompt = string.IsNullOrWhiteSpace(prompt)
			? $"Enter option ({string.Join("/", _currentOptions.Select((myCliChoice option) => option.Key))}): "
			: prompt;
		while (true)
		{
			string? input = ReadInput(resolvedPrompt, channel);
			if (input == null)
			{
				LogSelection(null, null);
				return null;
			}
			string trimmed = input.Trim().TrimStart('\uFEFF');
			if (trimmed.Length == 0)
			{
				WriteLine("Invalid input.", channel);
				continue;
			}
			if (tryHandleExit != null && tryHandleExit(trimmed))
			{
				LogSelection(trimmed, null);
				return null;
			}
			myCliChoice? match = _currentOptions.FirstOrDefault((myCliChoice option) =>
				option.Key.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
				|| option.Aliases.Any((string alias) => alias.Equals(trimmed, StringComparison.OrdinalIgnoreCase)));
			if (match != null)
			{
				LogSelection(trimmed, match);
				return match;
			}
			if (tryHandleGlobalCommand != null)
			{
				bool? result = tryHandleGlobalCommand(trimmed);
				if (result == true)
				{
					ShowChoices(_currentOptions, channel);
					continue;
				}
				if (result == null)
				{
					return null;
				}
			}
			WriteLine("Invalid input.", channel);
		}
	}

	private void LogOptions()
	{
		string rendered = string.Join(" | ", _currentOptions.Select((myCliChoice option) => $"{option.Key}={option.Text}"));
		WriteAudit($"[CLI_OPTIONS] {rendered}");
	}

	private void LogSelection(string? input, myCliChoice? choice)
	{
		if (choice == null)
		{
			WriteAudit($"[CLI_SELECTION] input={(input ?? "(null)")} index=(none)");
			return;
		}
		WriteAudit($"[CLI_SELECTION] input={input} index={choice.Index} key={choice.Key} text={choice.Text}");
	}

	private void WriteAudit(string message)
	{
		_auditWriter?.Invoke(message);
	}
}
