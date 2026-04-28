using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace MegaCrit.Sts2.Core;

/// <summary>
/// Handles bidirectional binary-protocol communication over a named pipe.
///
/// Protocol: length-prefixed frames
///   [4 bytes LE: msgType] [4 bytes LE: payloadLen] [payloadLen bytes: payload]
///
/// Kernel -> Client messages:
///   0x01 TextOutput      UTF-8 text line
///   0x02 PromptInput     UTF-8 prompt text; client should send InputResponse
///   0x03 ShowChoices     UTF-8 lines in the form "key|text"
///   0x04 Exit            empty payload
///
/// Client -> Kernel messages:
///   0x10 InputResponse   UTF-8 user text
///   0x11 ChoiceResponse  decimal integer as UTF-8 text
///   0x12 SyncRunSave     raw current_run.save bytes, written to the kernel save slot
/// </summary>
public sealed class myBinaryPipeHandler : IDisposable
{
	private const int MsgTextOutput = 0x01;
	private const int MsgPromptInput = 0x02;
	private const int MsgShowChoices = 0x03;
	private const int MsgExit = 0x04;

	private const int MsgInputResponse = 0x10;
	private const int MsgChoiceResponse = 0x11;
	private const int MsgSyncRunSave = 0x12;

	private const int HeaderSize = 8;

	private readonly NamedPipeServerStream _pipe;
	private bool _disposed;

	public myBinaryPipeHandler(string pipeName)
	{
		_pipe = CreatePipeServer(pipeName);
	}

	private static NamedPipeServerStream CreatePipeServer(string pipeName)
	{
		PipeSecurity security = new();

		SecurityIdentifier? currentUserSid = WindowsIdentity.GetCurrent().User;
		if (currentUserSid != null)
		{
			security.AddAccessRule(new PipeAccessRule(
				currentUserSid,
				PipeAccessRights.FullControl,
				AccessControlType.Allow));
		}

		security.AddAccessRule(new PipeAccessRule(
			new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
			PipeAccessRights.ReadWrite,
			AccessControlType.Allow));

		security.AddAccessRule(new PipeAccessRule(
			new SecurityIdentifier(WellKnownSidType.WorldSid, null),
			PipeAccessRights.ReadWrite,
			AccessControlType.Allow));

		return NamedPipeServerStreamAcl.Create(
			pipeName,
			PipeDirection.InOut,
			1,
			PipeTransmissionMode.Byte,
			PipeOptions.None,
			0,
			0,
			security);
	}

	public void WaitForConnection()
	{
		_pipe.WaitForConnection();
	}

	private void WriteMessage(int msgType, byte[] payload)
	{
		byte[] header = new byte[HeaderSize];
		BitConverter.TryWriteBytes(header.AsSpan(0, 4), msgType);
		BitConverter.TryWriteBytes(header.AsSpan(4, 4), payload.Length);
		_pipe.Write(header, 0, HeaderSize);
		if (payload.Length > 0)
		{
			_pipe.Write(payload, 0, payload.Length);
		}
		_pipe.Flush();
	}

	private (int msgType, byte[] payload) ReadMessage()
	{
		byte[] header = new byte[HeaderSize];
		ReadExact(header);
		int msgType = BitConverter.ToInt32(header, 0);
		int payloadLength = BitConverter.ToInt32(header, 4);
		byte[] payload = new byte[payloadLength];
		if (payloadLength > 0)
		{
			ReadExact(payload);
		}
		return (msgType, payload);
	}

	private void ReadExact(byte[] buffer)
	{
		int offset = 0;
		while (offset < buffer.Length)
		{
			int read = _pipe.Read(buffer, offset, buffer.Length - offset);
			if (read == 0)
			{
				throw new IOException("Pipe closed by client.");
			}
			offset += read;
		}
	}

	private static byte[] Encode(string text) => Encoding.UTF8.GetBytes(text);

	private static string Decode(byte[] bytes) => Encoding.UTF8.GetString(bytes);

	public void SendTextOutput(string text)
	{
		WriteMessage(MsgTextOutput, Encode(text));
	}

	public void SendPromptInput(string prompt)
	{
		WriteMessage(MsgPromptInput, Encode(prompt));
	}

	public void SendShowChoices(System.Collections.Generic.IReadOnlyList<myCliChoice> choices)
	{
		StringBuilder builder = new();
		for (int i = 0; i < choices.Count; i++)
		{
			if (i > 0)
			{
				builder.Append('\n');
			}

			builder.Append(choices[i].Key);
			builder.Append('|');
			builder.Append(choices[i].Text);
		}

		WriteMessage(MsgShowChoices, Encode(builder.ToString()));
	}

	public void SendExit()
	{
		WriteMessage(MsgExit, Array.Empty<byte>());
	}

	public string ReadInputResponse()
	{
		while (true)
		{
			(int msgType, byte[] payload) = ReadMessage();
			if (msgType == MsgSyncRunSave)
			{
				WriteSyncedRunSave(payload);
				continue;
			}

			if (msgType != MsgInputResponse)
			{
				throw new InvalidOperationException($"Expected InputResponse (0x10), got 0x{msgType:X2}.");
			}

			return Decode(payload);
		}
	}

	public int ReadChoiceResponse()
	{
		while (true)
		{
			(int msgType, byte[] payload) = ReadMessage();
			if (msgType == MsgSyncRunSave)
			{
				WriteSyncedRunSave(payload);
				continue;
			}

			if (msgType != MsgChoiceResponse)
			{
				throw new InvalidOperationException($"Expected ChoiceResponse (0x11), got 0x{msgType:X2}.");
			}

			string text = Decode(payload).Trim();
			if (!int.TryParse(text, out int index))
			{
				throw new FormatException($"ChoiceResponse payload is not a valid integer: \"{text}\".");
			}

			return index;
		}
	}

	private static void WriteSyncedRunSave(byte[] payload)
	{
		if (payload == null || payload.Length == 0)
		{
			throw new InvalidOperationException("SyncRunSave payload was empty.");
		}

		string savePath = SaveManager.Instance.GetProfileScopedPath(
			Path.Combine(UserDataPathProvider.SavesDir, RunSaveManager.runSaveFileName));
		string? directory = Path.GetDirectoryName(savePath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		File.WriteAllBytes(savePath, payload);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		try
		{
			_pipe.Dispose();
		}
		catch
		{
		}
	}
}
