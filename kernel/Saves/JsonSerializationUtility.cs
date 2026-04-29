using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rngs;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Settings;

namespace MegaCrit.Sts2.Core.Saves;

public static class JsonSerializationUtility
{
	public static IJsonTypeInfoResolver DefaultResolver { get; } = new DefaultJsonTypeInfoResolver();

	public static JsonSerializerOptions Options { get; } = CreateOptions();

	private static JsonSerializerOptions CreateOptions()
	{
		JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.General)
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			WriteIndented = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
			IncludeFields = true,
			TypeInfoResolver = new DefaultJsonTypeInfoResolver()
				.WithAddedModifier(AlphabetizeProperties)
				.WithAddedModifier(JsonSerializeConditionAttribute.CheckJsonSerializeConditionsModifier)
		};

		// Register ModelId converter (flat string format "Category.Entry")
		options.Converters.Add(new ModelIdRunSaveConverter());

		// Register snake_case enum converters (matching original MegaCritSerializerContext)
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<Achievement>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<AspectRatioSetting>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<VSyncType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<GameMode>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<RelicRarity>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<RunRngType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<PlayerRngType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<MapPointType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<ModSource>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<PlatformType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<RewardType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<RoomType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<EpochState>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<FastModeType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<CardCreationSource>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<CardRarityOddsType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<ControllerMappingType>());
		options.Converters.Add(new SnakeCaseJsonStringEnumConverter<BadgeRarity>());

		// DynamicVarType uses PascalCase (not snake_case), matching original SerializableDynamicVarDictionarySerializerContext
		options.Converters.Add(new JsonStringEnumConverter<DynamicVarType>());

		return options;
	}

	public static void AddTypeInfoResolver(IJsonTypeInfoResolver resolver)
	{
		Options.TypeInfoResolverChain.Add(resolver);
	}

	public static async Task<string> SerializeAsync<T>(T data) where T : ISaveSchema
	{
		using MemoryStream stream = new MemoryStream();
		await JsonSerializer.SerializeAsync((Stream)stream, data, GetTypeInfo<T>(), default(CancellationToken));
		stream.Position = 0L;
		using StreamReader reader = new StreamReader(stream);
		return await reader.ReadToEndAsync();
	}

	public static JsonTypeInfo<T> GetTypeInfo<T>(T value)
	{
		return (JsonTypeInfo<T>)Options.GetTypeInfo(typeof(T));
	}

	public static JsonTypeInfo<T> GetTypeInfo<T>()
	{
		return (JsonTypeInfo<T>)Options.GetTypeInfo(typeof(T));
	}

	public static void AlphabetizeProperties(JsonTypeInfo info)
	{
		if (info.Kind == JsonTypeInfoKind.Object)
		{
			List<JsonPropertyInfo> list = new List<JsonPropertyInfo>();
			list.AddRange(info.Properties);
			list.Sort((JsonPropertyInfo p1, JsonPropertyInfo p2) => string.CompareOrdinal(p1.Name, p2.Name));
			info.Properties.Clear();
			for (int num = 0; num < list.Count; num++)
			{
				list[num].Order = num;
				info.Properties.Add(list[num]);
			}
		}
	}

	public static string ToJson<T>(T obj) where T : ISaveSchema
	{
		return JsonSerializer.Serialize(obj, GetTypeInfo<T>());
	}

	public static ReadSaveResult<T> FromJson<T>(string json) where T : ISaveSchema, new()
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			Log.Error($"The json for type={typeof(T)} was empty!");
			return new ReadSaveResult<T>(ReadSaveStatus.FileEmpty);
		}
		try
		{
			T val = JsonSerializer.Deserialize(json, GetTypeInfo<T>());
			if (val == null)
			{
				Log.Error($"Json parsed as null! type={typeof(T)}");
				return new ReadSaveResult<T>(ReadSaveStatus.JsonParseError);
			}
			return new ReadSaveResult<T>(val);
		}
		catch (JsonException ex)
		{
			string value = ex.Path ?? "unknown";
			Log.Error($"Failed to deserialize type={typeof(T)} at path={value}, line={ex.LineNumber}, position={ex.BytePositionInLine}: {ex.Message}");
			return new ReadSaveResult<T>(ReadSaveStatus.JsonParseError, $"JSON error at {value} (line {ex.LineNumber}): {ex.Message}");
		}
	}
}
