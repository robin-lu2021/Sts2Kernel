using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Exceptions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Commands;

public static class CardSelectCmd
{
	private sealed class StackedSelectorScope : IDisposable
	{
		private readonly ICardSelector _selector;

		private bool _disposed;

		public StackedSelectorScope(ICardSelector selector)
		{
			_selector = selector;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			if (_selectorStack.Count > 0 && _selectorStack.Peek() == _selector)
			{
				_selectorStack.Pop();
			}
		}
	}

	private sealed class SelectorScope : IDisposable
	{
		private bool _disposed;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_selectorStack.Clear();
		}
	}

	private static readonly char[] _selectionSeparators = new char[6] { ' ', ',', '，', ';', '；', '/' };

	private static readonly Stack<ICardSelector> _selectorStack = new();

	public static ICardSelector? Selector => _selectorStack.Count > 0 ? _selectorStack.Peek() : null;

	public static void Reset()
	{
		_selectorStack.Clear();
	}

	public static IDisposable UseSelector(ICardSelector selector)
	{
		if (_selectorStack.Count > 0)
		{
			throw new InvalidOperationException("A card selector is already active.");
		}
		_selectorStack.Push(selector);
		return new SelectorScope();
	}

	public static IDisposable PushSelector(ICardSelector selector)
	{
		_selectorStack.Push(selector);
		return new StackedSelectorScope(selector);
	}

	private static List<CardModel> SelectDeterministically(IEnumerable<CardModel> source, CardSelectorPrefs prefs)
	{
		List<CardModel> cards = source.ToList();
		if (cards.Count == 0)
		{
			return cards;
		}
		if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
		{
			return cards;
		}
		int min = Math.Max(0, prefs.MinSelect);
		int max = prefs.MaxSelect <= 0 ? cards.Count : prefs.MaxSelect;
		int count = Math.Max(min, 1);
		count = Math.Min(cards.Count, Math.Min(max, count));
		return cards.Take(count).ToList();
	}

	private static void ReportSoftlock()
	{
		;
	}

	public static CardModel? FromChooseACardScreen(PlayerChoiceContext context, IReadOnlyList<CardModel> cards, Player player, bool canSkip = false)
	{
		if (cards.Count == 0)
		{
			ReportSoftlock();
			return null;
		}
		CardModel? result;
		if (Selector != null)
		{
			result = Selector.GetSelectedCards(cards, 0, 1).GetAwaiter().GetResult().FirstOrDefault();
		}
		else
		{
			context.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);
			try
			{
				result = PromptForSingleCard(cards, "[CHOOSE A CARD]", "Select card", canSkip);
			}
			finally
			{
				context.SignalPlayerChoiceEnded();
			}
		}
		LogChoice(player, result == null ? Array.Empty<CardModel?>() : new CardModel?[] { result });
		return result;
	}

	public static IEnumerable<CardModel> FromSimpleGridForRewards(PlayerChoiceContext context, List<CardCreationResult> cards, Player player, CardSelectorPrefs prefs)
	{
		if (cards.Count == 0)
		{
			ReportSoftlock();
			return Array.Empty<CardModel>();
		}
		List<CardModel> options = cards.Select((CardCreationResult c) => c.Card).OfType<CardModel>().ToList();
		List<CardModel> result;
		if (!prefs.RequireManualConfirmation && options.Count <= prefs.MinSelect)
		{
			result = options.ToList();
		}
		else if (Selector != null)
		{
			result = Selector.GetSelectedCards(options, prefs.MinSelect, prefs.MaxSelect).GetAwaiter().GetResult().ToList();
		}
		else
		{
			context.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);
			try
			{
				result = PromptForCardSelection(options, prefs, "Choose reward cards");
			}
			finally
			{
				context.SignalPlayerChoiceEnded();
			}
		}
		LogChoice(player, result);
		return result;
	}

	public static IEnumerable<CardModel> FromSimpleGrid(PlayerChoiceContext context, IReadOnlyList<CardModel> cardsIn, Player player, CardSelectorPrefs prefs)
	{
		List<CardModel> cards = cardsIn.ToList();
		if (cards.Count == 0)
		{
			return Array.Empty<CardModel>();
		}
		List<CardModel> result;
		if (CombatManager.Instance.IsEnding)
		{
			return Array.Empty<CardModel>();
		}
		if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
		{
			result = cards.ToList();
		}
		else if (Selector != null)
		{
			result = Selector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect).GetAwaiter().GetResult().ToList();
		}
		else
		{
			context.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);
			try
			{
				result = PromptForCardSelection(cards, prefs, "Choose cards");
			}
			finally
			{
				context.SignalPlayerChoiceEnded();
			}
		}
		LogChoice(player, result);
		return result;
	}

	public static IEnumerable<CardModel> FromDeckForUpgrade(Player player, CardSelectorPrefs prefs)
	{
		List<CardModel> list = PileType.Deck.GetPile(player).Cards.OfType<CardModel>().Where((CardModel c) => c.IsUpgradable).ToList();
		if (list.Count == 0)
		{
			return Array.Empty<CardModel>();
		}
		IEnumerable<CardModel> result;
		if (list.Count <= prefs.MinSelect && !prefs.RequireManualConfirmation)
		{
			result = list;
		}
		else if (Selector != null)
		{
			result = Selector.GetSelectedCards(list, prefs.MinSelect, prefs.MaxSelect).GetAwaiter().GetResult();
		}
		else
		{
			result = PromptForCardSelection(list, prefs, "Choose cards to upgrade");
		}
		LogChoice(player, result);
		return result;
	}

	public static IEnumerable<CardModel> FromDeckForTransformation(Player player, CardSelectorPrefs prefs, Func<CardModel, CardTransformation>? cardToTransformation = null)
	{
		List<CardModel> list = PileType.Deck.GetPile(player).Cards.OfType<CardModel>().Where((CardModel c) => c.Type != CardType.Quest && c.IsTransformable).ToList();
		if (list.Count == 0)
		{
			return Array.Empty<CardModel>();
		}
		IEnumerable<CardModel> result;
		if (list.Count <= prefs.MinSelect && !prefs.RequireManualConfirmation)
		{
			result = list;
		}
		else if (Selector != null)
		{
			result = Selector.GetSelectedCards(list, prefs.MinSelect, prefs.MaxSelect).GetAwaiter().GetResult();
		}
		else
		{
			result = PromptForCardSelection(list, prefs, "Choose cards to transform");
		}
		LogChoice(player, result);
		return result;
	}

	public static IEnumerable<CardModel> FromDeckForEnchantment(Player player, EnchantmentModel enchantment, int amount, CardSelectorPrefs prefs)
	{
		return FromDeckForEnchantment(player, enchantment, amount, null, prefs);
	}

	public static IEnumerable<CardModel> FromDeckForEnchantment(Player player, EnchantmentModel enchantment, int amount, Func<CardModel?, bool>? additionalFilter, CardSelectorPrefs prefs)
	{
		IReadOnlyList<CardModel> cards = PileType.Deck.GetPile(player).Cards
			.OfType<CardModel>()
			.Where((CardModel c) => enchantment.CanEnchant(c) && (additionalFilter?.Invoke(c) ?? true))
			.ToList();
		return FromDeckForEnchantment(cards, enchantment, amount, prefs);
	}

	public static IEnumerable<CardModel> FromDeckForEnchantment(IReadOnlyList<CardModel> cards, EnchantmentModel enchantment, int amount, CardSelectorPrefs prefs)
	{
		if (cards.Any((CardModel c) => c.Pile?.Type != PileType.Deck || !enchantment.CanEnchant(c)))
		{
			throw new ArgumentException("All cards must be in the player's deck and enchantable.");
		}
		if (cards.Count == 0)
		{
			return Array.Empty<CardModel>();
		}
		Player player = cards[0].Owner;
		if (player.Creature.IsDead)
		{
			return Array.Empty<CardModel>();
		}
		IEnumerable<CardModel> result;
		if (cards.Count <= prefs.MinSelect)
		{
			result = cards;
		}
		else
		{
			Dictionary<CardModel, int> indexMap = PileType.Deck.GetPile(player).Cards
				.OfType<CardModel>()
				.Select((CardModel card, int index) => new KeyValuePair<CardModel, int>(card, index))
				.ToDictionary((KeyValuePair<CardModel, int> pair) => pair.Key, (KeyValuePair<CardModel, int> pair) => pair.Value);
			List<CardModel> orderedCards = cards.OrderBy((CardModel c) => indexMap.TryGetValue(c, out int index) ? index : int.MaxValue).ToList();
			if (Selector != null)
			{
				result = Selector.GetSelectedCards(orderedCards, prefs.MinSelect, prefs.MaxSelect).GetAwaiter().GetResult();
			}
			else
			{
				result = PromptForCardSelection(orderedCards, prefs, "Choose cards to enchant");
			}
		}
		LogChoice(player, result);
		return result;
	}

	public static IEnumerable<CardModel> FromDeckForRemoval(Player player, CardSelectorPrefs prefs, Func<CardModel, bool>? filter = null)
	{
		List<CardModel> deck = PileType.Deck.GetPile(player).Cards.OfType<CardModel>().ToList();
		return FromDeckGeneric(player, prefs, (CardModel c) => c.IsRemovable && (filter == null || filter(c)), (CardModel c) => c.Type != CardType.Curse ? deck.IndexOf(c) : -999999999);
	}

	public static IEnumerable<CardModel> FromDeckGeneric(Player player, CardSelectorPrefs prefs, Func<CardModel, bool>? filter = null, Func<CardModel, int>? sortingOrder = null)
	{
		List<CardModel> source = PileType.Deck.GetPile(player).Cards.OfType<CardModel>().ToList();
		List<CardModel> list = filter == null ? source.ToList() : source.Where(filter).ToList();
		if (player.Creature.IsDead)
		{
			return Array.Empty<CardModel>();
		}
		if (sortingOrder != null)
		{
			list = list.OrderBy(sortingOrder).ToList();
		}
		if (list.Count == 0)
		{
			return Array.Empty<CardModel>();
		}
		IEnumerable<CardModel> result;
		if (!prefs.RequireManualConfirmation && list.Count <= prefs.MinSelect)
		{
			result = list;
		}
		else if (Selector != null)
		{
			result = Selector.GetSelectedCards(list, prefs.MinSelect, prefs.MaxSelect).GetAwaiter().GetResult();
		}
		else
		{
			result = PromptForCardSelection(list, prefs, "Choose cards");
		}
		LogChoice(player, result);
		return result;
	}

	public static IEnumerable<CardModel> FromHand(PlayerChoiceContext context, Player player, CardSelectorPrefs prefs, Func<CardModel, bool>? filter, AbstractModel source)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return Array.Empty<CardModel>();
		}
		List<CardModel> list = PileType.Hand.GetPile(player).Cards.OfType<CardModel>().Where(filter ?? (_ => true)).ToList();
		IEnumerable<CardModel> result;
		if (list.Count == 0)
		{
			result = list;
		}
		else if (!prefs.RequireManualConfirmation && list.Count <= prefs.MinSelect)
		{
			result = list;
		}
		else if (Selector != null)
		{
			result = Selector.GetSelectedCards(list, prefs.MinSelect, prefs.MaxSelect).GetAwaiter().GetResult();
		}
		else
		{
			context.SignalPlayerChoiceBegun(PlayerChoiceOptions.CancelPlayCardActions);
			try
			{
				result = PromptForCardSelection(list, prefs, "Choose cards");
			}
			finally
			{
				context.SignalPlayerChoiceEnded();
			}
		}
		LogChoice(player, result);
		return result;
	}

	public static IEnumerable<CardModel> FromHandForDiscard(PlayerChoiceContext context, Player player, CardSelectorPrefs prefs, Func<CardModel, bool>? filter, AbstractModel source)
	{
		prefs.ShouldGlowGold = delegate(CardModel c)
		{
			if (!c.IsSlyThisTurn)
			{
				return false;
			}
			return c.CanPlay(out UnplayableReason reason, out _) || reason.HasResourceCostReason();
		};
		return FromHand(context, player, prefs, filter, source);
	}

	public static CardModel? FromHandForUpgrade(PlayerChoiceContext context, Player player, AbstractModel source)
	{
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return null;
		}
		List<CardModel> list = PileType.Hand.GetPile(player).Cards.OfType<CardModel>().Where((CardModel c) => c.IsUpgradable).ToList();
		CardModel? result;
		if (list.Count <= 1)
		{
			result = list.FirstOrDefault();
		}
		else if (Selector != null)
		{
			result = Selector.GetSelectedCards(list, 1, 1).GetAwaiter().GetResult().FirstOrDefault();
		}
		else
		{
			context.SignalPlayerChoiceBegun(PlayerChoiceOptions.CancelPlayCardActions);
			try
			{
				result = PromptForSingleCard(list, ResolvePromptText(new LocString("gameplay_ui", "CHOOSE_CARD_UPGRADE_HEADER"), "Choose card to upgrade"), "Select card", canSkip: false);
			}
			finally
			{
				context.SignalPlayerChoiceEnded();
			}
		}
		LogChoice(player, result == null ? Array.Empty<CardModel?>() : new CardModel?[] { result });
		return result;
	}

	public static IEnumerable<CardModel> FromChooseABundleScreen(Player player, IReadOnlyList<IReadOnlyList<CardModel>> bundles)
	{
		if (bundles.Count == 0)
		{
			return Array.Empty<CardModel>();
		}
		if (bundles.Count == 0)
		{
			ReportSoftlock();
			return Array.Empty<CardModel>();
		}
		IReadOnlyList<CardModel> result = PromptForBundleSelection(bundles);
		LogChoice(player, result);
		return result;
	}

	private static List<CardModel> PromptForCardSelection(IReadOnlyList<CardModel> cards, CardSelectorPrefs prefs, string fallbackPrompt)
	{
		if (cards.Count == 0)
		{
			return new List<CardModel>();
		}
		int minSelect = Math.Max(0, prefs.MinSelect);
		int maxSelect = prefs.MaxSelect <= 0 ? cards.Count : Math.Min(cards.Count, prefs.MaxSelect);
		bool canSkip = prefs.Cancelable || minSelect == 0;
		string header = ResolvePromptText(prefs.Prompt, fallbackPrompt);
		ShowCardSelectionOptions(cards, prefs, header, canSkip);
		while (true)
		{
			Program.Prompt(BuildCardSelectionPrompt(minSelect, maxSelect, cards.Count, canSkip));
			string? input = Program.ReadLine();
			if (input == null)
			{
				List<CardModel> fallback = canSkip ? new List<CardModel>() : SelectDeterministically(cards, prefs);
				AuditSelection(null, cards, fallback, canSkip);
				return fallback;
			}
			string trimmed = input.Trim().TrimStart('\uFEFF');
			if (trimmed.Length == 0)
			{
				Program.Write("Invalid input.");
				continue;
			}
			if (canSkip && IsSkipInput(trimmed))
			{
				AuditSelection(trimmed, cards, Array.Empty<CardModel>(), canSkip);
				return new List<CardModel>();
			}
			if (TryParseSelectedCards(trimmed, cards, minSelect, maxSelect, out List<CardModel>? selected))
			{
				AuditSelection(trimmed, cards, selected, canSkip);
				return selected;
			}
			if (Program.TryHandleGlobalInput(trimmed))
			{
				ShowCardSelectionOptions(cards, prefs, header, canSkip);
				continue;
			}
			Program.Write("Invalid input.");
		}
	}

	private static CardModel? PromptForSingleCard(IReadOnlyList<CardModel> cards, string header, string promptLabel, bool canSkip)
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1)
		{
			Cancelable = canSkip
		};
		ShowCardSelectionOptions(cards, prefs, header, canSkip);
		while (true)
		{
			Program.Prompt($"{promptLabel} (1-{cards.Count}" + (canSkip ? ", 0 to skip" : "") + "): ");
			string? input = Program.ReadLine();
			if (input == null)
			{
				CardModel? fallback = canSkip ? null : cards[0];
				AuditSelection(null, cards, fallback == null ? Array.Empty<CardModel>() : new[] { fallback }, canSkip);
				return fallback;
			}
			string trimmed = input.Trim().TrimStart('\uFEFF');
			if (trimmed.Length == 0)
			{
				Program.Write("Invalid input.");
				continue;
			}
			if (canSkip && IsSkipInput(trimmed))
			{
				AuditSelection(trimmed, cards, Array.Empty<CardModel>(), canSkip);
				return null;
			}
			if (int.TryParse(trimmed, out int index) && index >= 1 && index <= cards.Count)
			{
				CardModel result = cards[index - 1];
				AuditSelection(trimmed, cards, new[] { result }, canSkip);
				return result;
			}
			if (Program.TryHandleGlobalInput(trimmed))
			{
				ShowCardSelectionOptions(cards, prefs, header, canSkip);
				continue;
			}
			Program.Write("Invalid input.");
		}
	}

	private static IReadOnlyList<CardModel> PromptForBundleSelection(IReadOnlyList<IReadOnlyList<CardModel>> bundles)
	{
		ShowBundleOptions(bundles);
		while (true)
		{
			Program.Prompt($"Select bundle (1-{bundles.Count}): ");
			string? input = Program.ReadLine();
			if (input == null)
			{
				AuditBundleSelection(null, -1, null);
				return Array.Empty<CardModel>();
			}
			string trimmed = input.Trim().TrimStart('\uFEFF');
			if (trimmed.Length == 0)
			{
				Program.Write("Invalid input.");
				continue;
			}
			if (int.TryParse(trimmed, out int index) && index >= 1 && index <= bundles.Count)
			{
				IReadOnlyList<CardModel> result = bundles[index - 1];
				AuditBundleSelection(trimmed, index, result);
				return result;
			}
			if (Program.TryHandleGlobalInput(trimmed))
			{
				ShowBundleOptions(bundles);
				continue;
			}
			Program.Write("Invalid input.");
		}
	}

	private static void ShowCardSelectionOptions(IReadOnlyList<CardModel> cards, CardSelectorPrefs prefs, string header, bool canSkip)
	{
		Program.Write(header);
		for (int i = 0; i < cards.Count; i++)
		{
			Program.Write($"  {i + 1}. {FormatCardOption(cards[i], prefs)}");
		}
		if (canSkip)
		{
			Program.Write("  0. Skip");
		}
		string audit = string.Join(" | ", cards.Select((CardModel card, int index) => $"{index + 1}={FormatCardOption(card, prefs)}"));
		if (canSkip)
		{
			audit += " | 0=Skip";
		}
		Program.Audit("[CLI_OPTIONS] card_select " + audit);
	}

	private static void ShowBundleOptions(IReadOnlyList<IReadOnlyList<CardModel>> bundles)
	{
		Program.Write("[CHOOSE A BUNDLE]");
		for (int i = 0; i < bundles.Count; i++)
		{
			Program.Write($"  {i + 1}. {FormatBundleOption(bundles[i])}");
		}
		Program.Audit("[CLI_OPTIONS] bundle_select " + string.Join(" | ", bundles.Select((IReadOnlyList<CardModel> bundle, int index) => $"{index + 1}={FormatBundleOption(bundle)}")));
	}

	private static bool TryParseSelectedCards(string input, IReadOnlyList<CardModel> cards, int minSelect, int maxSelect, out List<CardModel>? selected)
	{
		selected = null;
		List<int> indexes = new List<int>();
		foreach (string token in input.Split(_selectionSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (!int.TryParse(token, out int index) || index < 1 || index > cards.Count)
			{
				return false;
			}
			if (indexes.Contains(index))
			{
				return false;
			}
			indexes.Add(index);
		}
		if (indexes.Count < minSelect || indexes.Count > maxSelect)
		{
			return false;
		}
		selected = indexes.Select((int index) => cards[index - 1]).ToList();
		return indexes.Count > 0 || minSelect == 0;
	}

	private static string BuildCardSelectionPrompt(int minSelect, int maxSelect, int totalCount, bool canSkip)
	{
		if (minSelect == maxSelect)
		{
			if (maxSelect <= 1)
			{
				return $"Select card (1-{totalCount}" + (canSkip ? ", 0 to skip" : "") + "): ";
			}
			return $"Select {maxSelect} cards separated by spaces (1-{totalCount}" + (canSkip ? ", or 0 to skip" : "") + "): ";
		}
		return $"Select {minSelect}-{maxSelect} cards separated by spaces (1-{totalCount}" + (canSkip ? ", or 0 to skip" : "") + "): ";
	}

	private static bool IsSkipInput(string input)
	{
		return input == "0" || input.Equals("skip", StringComparison.OrdinalIgnoreCase);
	}

	private static string ResolvePromptText(LocString prompt, string fallback)
	{
		try
		{
			if (!prompt.IsEmpty && prompt.Exists())
			{
				string text = prompt.GetFormattedText();
				if (!string.IsNullOrWhiteSpace(text) && text.IndexOfAny(new char[2] { '{', '[' }) < 0)
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return fallback;
	}

	private static string FormatCardOption(CardModel card, CardSelectorPrefs prefs)
	{
		string label = string.IsNullOrWhiteSpace(card.Title) ? card.Id.Entry : card.Title;
		if (prefs.ShouldGlowGold?.Invoke(card) == true)
		{
			return label + " [gold]";
		}
		return label;
	}

	private static string FormatBundleOption(IReadOnlyList<CardModel> bundle)
	{
		if (bundle.Count == 0)
		{
			return "(empty)";
		}
		return string.Join(", ", bundle.Select((CardModel card) => string.IsNullOrWhiteSpace(card.Title) ? card.Id.Entry : card.Title));
	}

	private static void AuditSelection(string? input, IReadOnlyList<CardModel> options, IReadOnlyList<CardModel> selected, bool canSkip)
	{
		if (selected.Count == 0)
		{
			Program.Audit($"[CLI_SELECTION] input={(input ?? "(null)")} index=" + (canSkip ? "0" : "(none)") + (canSkip ? " key=0 text=Skip" : ""));
			return;
		}
		string indexText = string.Join(",", selected.Select((CardModel card) => FindOneBasedIndex(options, card)));
		string labelText = string.Join(" | ", selected.Select((CardModel card) => string.IsNullOrWhiteSpace(card.Title) ? card.Id.Entry : card.Title));
		Program.Audit($"[CLI_SELECTION] input={(input ?? "(null)")} index={indexText} key={indexText} text={labelText}");
	}

	private static void AuditBundleSelection(string? input, int oneBasedIndex, IReadOnlyList<CardModel>? selected)
	{
		if (selected == null || selected.Count == 0)
		{
			Program.Audit($"[CLI_SELECTION] input={(input ?? "(null)")} index=(none)");
			return;
		}
		Program.Audit($"[CLI_SELECTION] input={(input ?? "(null)")} index={oneBasedIndex} key={input} text={FormatBundleOption(selected)}");
	}

	private static int FindOneBasedIndex(IReadOnlyList<CardModel> options, CardModel selected)
	{
		for (int i = 0; i < options.Count; i++)
		{
			if (ReferenceEquals(options[i], selected))
			{
				return i + 1;
			}
		}
		return -1;
	}

	private static void LogChoice(Player player, IEnumerable<CardModel?> cards)
	{
		string value = string.Join(",", cards.OfType<CardModel>().Select((CardModel c) => c.Id));
		Log.Info($"Player {player.NetId} chose cards [{value}]");
	}
}
