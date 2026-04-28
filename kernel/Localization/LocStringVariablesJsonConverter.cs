using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Localization;

public class LocStringVariablesJsonConverter : JsonConverter<Dictionary<string, object>>
{
	public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		Dictionary<string, SerializableDynamicVar>? dictionary = JsonSerializer.Deserialize<Dictionary<string, SerializableDynamicVar>>(ref reader, options);
		if (dictionary == null)
		{
			throw new InvalidOperationException("Could not read LocString variables");
		}
		Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
		foreach (KeyValuePair<string, SerializableDynamicVar> item in dictionary)
		{
			dictionary2[item.Key] = item.Value.ToDynamicVar(item.Key);
		}
		return dictionary2;
	}

	public override void Write(Utf8JsonWriter writer, Dictionary<string, object> varDict, JsonSerializerOptions options)
	{
		Dictionary<string, SerializableDynamicVar> dictionary = new Dictionary<string, SerializableDynamicVar>();
		foreach (KeyValuePair<string, object> item in varDict)
		{
			SerializableDynamicVar? serializableDynamicVar = SerializableDynamicVar.FromDynamicVar(item.Value);
			if (serializableDynamicVar.HasValue && (serializableDynamicVar.Value.type != DynamicVarType.BaseDynamic || !(item.Value.GetType() != typeof(DynamicVar))))
			{
				dictionary[item.Key] = serializableDynamicVar.Value;
			}
		}
		JsonSerializer.Serialize(writer, dictionary, options);
	}
}
