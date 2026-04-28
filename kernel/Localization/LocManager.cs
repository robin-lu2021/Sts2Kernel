using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Localization;

public class LocManager
{
	private sealed class OverrideState
	{
		public required string Language { get; init; }

		public required Dictionary<string, LocTable> Tables { get; init; }
	}

	public delegate void LocaleChangeCallback();

	private readonly List<LocaleChangeCallback> _localeChangeCallbacks = new List<LocaleChangeCallback>();

	private Dictionary<string, LocTable> _tables = new Dictionary<string, LocTable>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, LocTable> _engTables = new Dictionary<string, LocTable>(StringComparer.OrdinalIgnoreCase);

	private OverrideState? _overrideState;

	public static LocManager Instance { get; private set; } = null!;

	public static List<string> Languages { get; } = DiscoverLanguages();

	public string Language { get; private set; } = "eng";

	public bool OverridesActive => _overrideState != null;

	public static void Initialize()
	{
		Instance = new LocManager();
	}

	public LocManager()
	{
		string language = SaveManager.Instance?.SettingsSave.Language ?? "eng";
		if (!Languages.Contains(language))
		{
			language = "eng";
		}
		SetLanguage(language);
	}

	public void SetLanguage(string language)
	{
		if (!Languages.Contains(language))
		{
			language = "eng";
		}
		string localizationRoot = FindLocalizationRoot();
		_engTables = LoadTables(localizationRoot, "eng", null);
		_tables = language == "eng" ? CloneTables(_engTables) : LoadTables(localizationRoot, language, _engTables);
		Language = language;
		if (SaveManager.Instance?.SettingsSave != null)
		{
			SaveManager.Instance.SettingsSave.Language = language;
		}
		NotifyLocaleChanged();
	}

	public void StartOverridingLanguageAsEnglish()
	{
		if (_overrideState != null)
		{
			return;
		}
		_overrideState = new OverrideState
		{
			Language = Language,
			Tables = _tables
		};
		_tables = CloneTables(_engTables);
		Language = "eng";
		NotifyLocaleChanged();
	}

	public void StopOverridingLanguageAsEnglish()
	{
		if (_overrideState == null)
		{
			return;
		}
		Language = _overrideState.Language;
		_tables = _overrideState.Tables;
		_overrideState = null;
		NotifyLocaleChanged();
	}

	public void SubscribeToLocaleChange(LocaleChangeCallback callback)
	{
		if (!_localeChangeCallbacks.Contains(callback))
		{
			_localeChangeCallbacks.Add(callback);
		}
	}

	public void UnsubscribeToLocaleChange(LocaleChangeCallback callback)
	{
		_localeChangeCallbacks.Remove(callback);
	}

	public LocTable GetTable(string tableName)
	{
		if (_tables.TryGetValue(tableName, out LocTable? table))
		{
			return table;
		}
		LocTable? fallback = _engTables.TryGetValue(tableName, out LocTable? fallbackTable) ? fallbackTable : null;
		table = new LocTable(tableName, new Dictionary<string, string>(StringComparer.Ordinal), fallback);
		_tables[tableName] = table;
		return table;
	}

	public string SmartFormat(LocString locString, Dictionary<string, object> variables)
	{
		return SimpleFormat(locString.GetRawText(), variables);
	}

	private void NotifyLocaleChanged()
	{
		foreach (LocaleChangeCallback callback in _localeChangeCallbacks.ToList())
		{
			callback();
		}
	}

	private static string SimpleFormat(string rawText, IReadOnlyDictionary<string, object> variables)
	{
		string text = rawText;
		foreach (KeyValuePair<string, object> variable in variables)
		{
			text = text.Replace("{" + variable.Key + "}", variable.Value?.ToString() ?? string.Empty, StringComparison.Ordinal);
		}
		return text;
	}

	private static Dictionary<string, LocTable> CloneTables(Dictionary<string, LocTable> source)
	{
		return source.ToDictionary(
			static pair => pair.Key,
			static pair => pair.Value,
			StringComparer.OrdinalIgnoreCase);
	}

	private static Dictionary<string, LocTable> LoadTables(string localizationRoot, string language, Dictionary<string, LocTable>? fallbackTables)
	{
		string languageDir = Path.Combine(localizationRoot, language);
		Dictionary<string, LocTable> tables = new Dictionary<string, LocTable>(StringComparer.OrdinalIgnoreCase);
		if (!Directory.Exists(languageDir))
		{
			Log.Warn($"Localization directory not found for language '{language}': {languageDir}");
			return fallbackTables != null ? CloneTables(fallbackTables) : tables;
		}

		foreach (string filePath in Directory.EnumerateFiles(languageDir, "*.json"))
		{
			string tableName = Path.GetFileNameWithoutExtension(filePath);
			Dictionary<string, string>? translations = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(filePath));
			LocTable? fallback = fallbackTables != null && fallbackTables.TryGetValue(tableName, out LocTable? fallbackTable) ? fallbackTable : null;
			tables[tableName] = new LocTable(tableName, translations ?? new Dictionary<string, string>(), fallback);
		}

		if (fallbackTables != null)
		{
			foreach (KeyValuePair<string, LocTable> fallback in fallbackTables)
			{
				tables.TryAdd(fallback.Key, fallback.Value);
			}
		}

		return tables;
	}

	private static List<string> DiscoverLanguages()
	{
		string localizationRoot = FindLocalizationRoot();
		if (!Directory.Exists(localizationRoot))
		{
			return new List<string> { "eng" };
		}
		List<string> languages = Directory.EnumerateDirectories(localizationRoot)
			.Select(Path.GetFileName)
			.Where(static name => !string.IsNullOrWhiteSpace(name))
			.Cast<string>()
			.OrderBy(static name => name)
			.ToList();
		if (!languages.Contains("eng"))
		{
			languages.Insert(0, "eng");
		}
		return languages;
	}

	private static string FindLocalizationRoot()
	{
		string[] candidates =
		{
			Path.Combine(Directory.GetCurrentDirectory(), "localization"),
			Path.Combine(AppContext.BaseDirectory, "localization")
		};

		foreach (string candidate in candidates)
		{
			if (Directory.Exists(candidate))
			{
				return candidate;
			}
		}

		DirectoryInfo? current = new DirectoryInfo(Directory.GetCurrentDirectory());
		while (current != null)
		{
			string candidate = Path.Combine(current.FullName, "localization");
			if (Directory.Exists(candidate))
			{
				return candidate;
			}
			current = current.Parent;
		}

		return Path.Combine(Directory.GetCurrentDirectory(), "localization");
	}
}
