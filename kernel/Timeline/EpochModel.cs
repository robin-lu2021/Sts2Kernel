using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core.Timeline;

public class EpochModel
{
	private static IReadOnlyList<string>? _allEpochIds;

	private static readonly Dictionary<string, Type> _epochTypeDictionary = new Dictionary<string, Type>();

	private static readonly Dictionary<Type, string> _typeToIdDictionary = new Dictionary<Type, string>();

	public static IReadOnlyList<string> AllEpochIds => _allEpochIds ??= EpochModelSubtypes.All.Select(GetId).ToList();

	public string Year => new LocString("eras", StringHelper.Slugify(Era.ToString()) + ".year").GetFormattedText();

	public string EraName => new LocString("eras", StringHelper.Slugify(Era.ToString()) + ".name").GetFormattedText();

	public virtual string Id => GetId(GetType());

	public ModelId ModelId => new ModelId("epoch", Id);

	public virtual bool IsArtPlaceholder => true;

	public LocString Title => new LocString("epochs", Id + ".title");

	public string Description => new LocString("epochs", Id + ".description").GetFormattedText();

	public virtual string? StoryId => null;

	public virtual string UnlockText => new LocString("epochs", Id + ".unlockText").GetFormattedText();

	public virtual EpochEra Era => EpochEra.Seeds0;

	public virtual int EraPosition => 0;

	static EpochModel()
	{
		for (int i = 0; i < EpochModelSubtypes.Count; i++)
		{
			Type type = EpochModelSubtypes.Get(i);
			EpochModel epochModel = (EpochModel)Activator.CreateInstance(type)!;
			_epochTypeDictionary[epochModel.Id] = type;
			_typeToIdDictionary[type] = epochModel.Id;
		}
	}

	public virtual EpochModel[] GetTimelineExpansion()
	{
		return Array.Empty<EpochModel>();
	}

	public virtual void QueueUnlocks()
	{
	}

	public static string GetId<T>() where T : EpochModel
	{
		return GetId(typeof(T));
	}

	public static string GetId(Type t)
	{
		if (_typeToIdDictionary.TryGetValue(t, out string? value))
		{
			return value;
		}
		string id = ToUpperSnakeCase(t.Name);
		_typeToIdDictionary[t] = id;
		_epochTypeDictionary[id] = t;
		return id;
	}

	public static bool IsValid(string id)
	{
		return AllEpochIds.Any((string epoch) => epoch.Equals(id, StringComparison.Ordinal));
	}

	public static EpochModel Get(string id)
	{
		if (_epochTypeDictionary.TryGetValue(id, out Type value))
		{
			return (EpochModel)Activator.CreateInstance(value)!;
		}
		throw new ArgumentException("Epoch with id '" + id + "' does not exist.");
	}

	public static EpochModel Get<T>() where T : EpochModel
	{
		return Get(GetId<T>());
	}

	protected static void QueueTimelineExpansion(EpochModel[] epochs)
	{
	}

	private static string ToUpperSnakeCase(string value)
	{
		StringBuilder builder = new StringBuilder(value.Length + 8);
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (char.IsUpper(c) && i > 0)
			{
				char previous = value[i - 1];
				bool nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);
				if (char.IsLower(previous) || char.IsDigit(previous) || nextIsLower)
				{
					builder.Append('_');
				}
			}
			builder.Append(char.ToUpperInvariant(c));
		}
		return builder.ToString();
	}
}
