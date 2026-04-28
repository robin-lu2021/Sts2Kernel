using System;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Helpers;

public static class OneTimeInitialization
{
	private static bool _initialized;

	private static bool _deferredExecuted;

	public static ReadSaveResult<SettingsSave> SettingsReadResult { get; private set; } = new(ReadSaveStatus.FileNotFound);

	public static void Execute()
	{
		ExecuteEssential();
		ExecuteDeferred();
	}

	public static void ExecuteEssential()
	{
		if (!_initialized)
		{
			_initialized = true;
			if (TestMode.IsOn)
			{
				SettingsReadResult = SaveManager.Instance.InitSettingsDataForTest();
			}
			else
			{
				SettingsReadResult = SaveManager.Instance.InitSettingsData();
			}
			LocManager.Initialize();
			SaveManager.Instance.InitProfileId(0);
			ModelDb.Init();
			ModelIdSerializationCache.Init();
			ModelDb.InitIds();
			HeadlessProgressDefaults.ApplyAllUnlocked(SaveManager.Instance.Progress);
		}
	}

	public static void ExecuteDeferred()
	{
		if (!_deferredExecuted)
		{
			_deferredExecuted = true;
			ModelDb.Preload();
			PrewarmJit();
		}
	}

	private static void PrewarmJit()
	{
		Type typeFromHandle = typeof(PacketWriter);
		Type typeFromHandle2 = typeof(PacketReader);
		foreach (Type subtype in ReflectionHelper.GetSubtypes<IPacketSerializable>())
		{
			RuntimeHelpers.PrepareMethod(subtype.GetMethod("Serialize").MethodHandle);
			RuntimeHelpers.PrepareMethod(subtype.GetMethod("Deserialize").MethodHandle);
			RuntimeHelpers.PrepareMethod(typeFromHandle.GetMethod("WriteList").MethodHandle, new RuntimeTypeHandle[1] { subtype.TypeHandle });
			RuntimeHelpers.PrepareMethod(typeFromHandle.GetMethod("Write").MethodHandle, new RuntimeTypeHandle[1] { subtype.TypeHandle });
			RuntimeHelpers.PrepareMethod(typeFromHandle2.GetMethod("ReadList").MethodHandle, new RuntimeTypeHandle[1] { subtype.TypeHandle });
			RuntimeHelpers.PrepareMethod(typeFromHandle2.GetMethod("Read").MethodHandle, new RuntimeTypeHandle[1] { subtype.TypeHandle });
		}
	}
}
