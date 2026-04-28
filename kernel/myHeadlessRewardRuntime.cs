using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using CoreEventModel = MegaCrit.Sts2.Core.EventModel;
using KernelEventModel = MegaCrit.Sts2.Core.EventModel;

namespace MegaCrit.Sts2.Core;

public enum myPendingRewardKind
{
	Gold,
	Potion,
	Relic,
	Card,
	SpecialCard,
	RemoveCard
}

public sealed class myPendingRewardEntry
{
	public myPendingRewardKind Kind { get; init; }

	public string Label { get; init; } = string.Empty;

	public int GoldAmount { get; init; }

	public PotionModel? Potion { get; init; }

	public RelicModel? Relic { get; init; }

	public IReadOnlyList<CardModel> Cards { get; init; } = Array.Empty<CardModel>();

	public bool CanSkip { get; init; } = true;

	public string BuildSummary(int oneBasedIndex)
	{
		StringBuilder builder = new StringBuilder();
		builder.Append(oneBasedIndex).Append(". ").Append(Label);
		if (Kind == myPendingRewardKind.Card || Kind == myPendingRewardKind.SpecialCard)
		{
			if (Cards.Count > 0)
			{
				builder.Append(" | options: ");
				builder.Append(string.Join(", ", Cards.Select((CardModel card, int idx) => $"{idx + 1}:{card.Id}")));
			}
		}
		return builder.ToString();
	}
}

public sealed class myPendingRewardState
{
	private readonly List<myPendingRewardEntry> _entries = new List<myPendingRewardEntry>();

	public string SourceLabel { get; }

	public IReadOnlyList<myPendingRewardEntry> Entries => _entries;

	public bool IsEmpty => _entries.Count == 0;

	public myPendingRewardState(string sourceLabel, IEnumerable<myPendingRewardEntry> entries)
	{
		SourceLabel = string.IsNullOrWhiteSpace(sourceLabel) ? "Rewards" : sourceLabel;
		_entries.AddRange(entries ?? Array.Empty<myPendingRewardEntry>());
	}

	public myPendingRewardEntry GetEntry(int zeroBasedIndex)
	{
		if (zeroBasedIndex < 0 || zeroBasedIndex >= _entries.Count)
		{
			throw new InvalidOperationException($"Reward index must be between 1 and {_entries.Count}.");
		}
		return _entries[zeroBasedIndex];
	}

	public void RemoveAt(int zeroBasedIndex)
	{
		GetEntry(zeroBasedIndex);
		_entries.RemoveAt(zeroBasedIndex);
	}

	public string BuildSummary()
	{
		StringBuilder builder = new StringBuilder();
		builder.Append(SourceLabel).Append(" pending rewards:");
		if (_entries.Count == 0)
		{
			builder.AppendLine();
			builder.Append("(none)");
			return builder.ToString();
		}
		for (int i = 0; i < _entries.Count; i++)
		{
			builder.AppendLine();
			builder.Append(_entries[i].BuildSummary(i + 1));
		}
		return builder.ToString();
	}
}

public static class myHeadlessRewardRuntime
{
	private static readonly FieldInfo? _relicRewardField = typeof(RelicReward).GetField("_relic", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo? _specialCardRewardField = typeof(SpecialCardReward).GetField("_card", BindingFlags.Instance | BindingFlags.NonPublic);

	public static Action<myPendingRewardState>? OfferedRewardsSink { get; set; }

	public static myPendingRewardState CreateCombatRewards(Player player, RunState runState, myCombatSession session)
	{
		if (player == null)
		{
			throw new ArgumentNullException(nameof(player));
		}
		if (runState == null)
		{
			throw new ArgumentNullException(nameof(runState));
		}
		if (session == null)
		{
			throw new ArgumentNullException(nameof(session));
		}

		return CreateCombatRewards(player, runState, session.Room, session.ExtraRewards);
	}

	public static myPendingRewardState CreateCombatRewards(Player player, RunState runState, CombatRoom room, IEnumerable<Reward>? extraRewards = null)
	{
		if (player == null)
		{
			throw new ArgumentNullException(nameof(player));
		}
		if (runState == null)
		{
			throw new ArgumentNullException(nameof(runState));
		}
		if (room == null)
		{
			throw new ArgumentNullException(nameof(room));
		}

		List<myPendingRewardEntry> entries = new List<myPendingRewardEntry>();
		List<Reward> rewards = new RewardsSet(player)
			.WithRewardsFromRoom(room)
			.GenerateWithoutOffering()
			.GetAwaiter()
			.GetResult();
		entries.AddRange(TranslateRewards(player, rewards));

		if (extraRewards != null)
		{
			List<Reward> extraRewardsList = extraRewards.ToList();
			if (extraRewardsList.Count > 0)
			{
				entries.AddRange(TranslateRewards(player, extraRewardsList));
			}
		}

		return new myPendingRewardState($"Combat rewards for {room.Encounter.Id.Entry}", entries);
	}

	public static myPendingRewardState CreateTreasureRewards(Player player, RunState runState, TreasureRoom room)
	{
		if (player == null)
		{
			throw new ArgumentNullException(nameof(player));
		}
		if (runState == null)
		{
			throw new ArgumentNullException(nameof(runState));
		}
		if (room == null)
		{
			throw new ArgumentNullException(nameof(room));
		}

		List<myPendingRewardEntry> entries = new List<myPendingRewardEntry>();
		if (ShouldGenerateTreasure(player))
		{
			int gold = player.PlayerRng.Rewards.NextInt(42, 53);
			if (runState.AscensionLevel >= (int)AscensionLevel.Poverty)
			{
				gold = (int)Math.Round((double)gold * MegaCrit.Sts2.Core.Helpers.AscensionHelper.PovertyAscensionGoldMultiplier);
			}
			entries.Add(new myPendingRewardEntry
			{
				Kind = myPendingRewardKind.Gold,
				Label = $"Gold x{gold}",
				GoldAmount = gold,
				CanSkip = false
			});

			EnsureRelicBagsAreReady(player, runState);
			RelicRarity rarity = RelicFactory.RollRarity(runState.Rng.TreasureRoomRelics);
			RelicModel relic = runState.SharedRelicGrabBag.PullFromFront(rarity, runState) ?? RelicFactory.FallbackRelic;
			entries.Add(new myPendingRewardEntry
			{
				Kind = myPendingRewardKind.Relic,
				Label = $"Relic {relic.Id.Entry}",
				Relic = relic.ToMutable(),
				CanSkip = false
			});
		}

		return new myPendingRewardState($"Treasure rewards for act {room.ActIndex + 1}", entries);
	}

	public static myPendingRewardState CreateRestSiteHealRewards(Player player, RunState runState)
	{
		if (player == null)
		{
			throw new ArgumentNullException(nameof(player));
		}
		if (runState == null)
		{
			throw new ArgumentNullException(nameof(runState));
		}

		decimal healAmount = (decimal)player.Creature.MaxHp * 0.3m;
		foreach (RelicModel relic in player.Relics)
		{
			healAmount = relic.ModifyRestSiteHealAmount(player.Creature, healAmount);
		}

		int healValue = Math.Max(0, (int)healAmount);
		player.Creature.SetCurrentHpInternal(player.Creature.CurrentHp + healValue);

		List<Reward> rewards = new List<Reward>();
		foreach (RelicModel relic in player.Relics)
		{
			relic.TryModifyRestSiteHealRewards(player, rewards, isMimicked: false);
			relic.AfterRestSiteHeal(player, isMimicked: false);
		}

		List<myPendingRewardEntry> entries = TranslateRewards(player, rewards);
		return new myPendingRewardState("Rest site results", entries);
	}

	public static bool TryCaptureOfferedRewards(Player player, AbstractRoom? room, IEnumerable<Reward> rewards, bool disallowSkipping = false)
	{
		Action<myPendingRewardState>? sink = OfferedRewardsSink;
		if (sink == null)
		{
			return false;
		}

		List<myPendingRewardEntry> entries = TranslateRewards(player, rewards);
		if (entries.Count == 0)
		{
			return false;
		}

		if (disallowSkipping)
		{
			entries = entries.Select(CloneAsNonSkippable).ToList();
		}

		sink(new myPendingRewardState(BuildOfferSourceLabel(room), entries));
		return true;
	}

	public static string ApplyReward(Player player, myPendingRewardEntry entry, int cardOptionZeroBased = -1)
	{
		if (player == null)
		{
			throw new ArgumentNullException(nameof(player));
		}
		if (entry == null)
		{
			throw new ArgumentNullException(nameof(entry));
		}

		switch (entry.Kind)
		{
		case myPendingRewardKind.Gold:
			if (entry.GoldAmount > 0)
			{
				PlayerCmd.GainGold(entry.GoldAmount, player).GetAwaiter().GetResult();
				return $"Gained {entry.GoldAmount} gold.";
			}
			return entry.Label;
		case myPendingRewardKind.Potion:
			if (entry.Potion == null)
			{
				throw new InvalidOperationException("Potion reward is missing its payload.");
			}
			if (!player.HasOpenPotionSlots)
			{
				throw new InvalidOperationException("No potion slot is available.");
			}
			PotionProcureResult potionResult = PotionCmd.TryToProcure(entry.Potion, player);
			if (!potionResult.success)
			{
				throw new InvalidOperationException("Failed to add the potion reward.");
			}
			return $"Obtained potion {entry.Potion.Id}.";
		case myPendingRewardKind.Relic:
			if (entry.Relic == null)
			{
				throw new InvalidOperationException("Relic reward is missing its payload.");
			}
			RelicCmd.Obtain(entry.Relic, player);
			return $"Obtained relic {entry.Relic.Id}.";
		case myPendingRewardKind.SpecialCard:
			if (entry.Cards.Count == 0)
			{
				throw new InvalidOperationException("Special card reward is empty.");
			}
			PrepareRewardCardForDeck(entry.Cards[0], player);
			CardPileAddResult specialCardResult = CardPileCmd.Add(entry.Cards[0], PileType.Deck);
			if (!specialCardResult.success)
			{
				throw new InvalidOperationException("Failed to add the special card reward.");
			}
			return $"Added card {entry.Cards[0].Id} to the deck.";
		case myPendingRewardKind.Card:
			if (entry.Cards.Count == 0)
			{
				throw new InvalidOperationException("Card reward has no options.");
			}
			if (cardOptionZeroBased < 0 || cardOptionZeroBased >= entry.Cards.Count)
			{
				throw new InvalidOperationException($"Card option must be between 1 and {entry.Cards.Count}.");
			}
			CardModel chosenCard = entry.Cards[cardOptionZeroBased];
			PrepareRewardCardForDeck(chosenCard, player);
			CardPileAddResult cardRewardResult = CardPileCmd.Add(chosenCard, PileType.Deck);
			if (!cardRewardResult.success)
			{
				throw new InvalidOperationException("Failed to add the card reward.");
			}
			return $"Added card {chosenCard.Id} to the deck.";
		case myPendingRewardKind.RemoveCard:
			throw new InvalidOperationException("Headless card removal rewards are not implemented yet.");
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private static List<Reward> BuildBaseCombatRewards(Player player, CombatRoom room, int ascensionLevel)
	{
		List<Reward> rewards = new List<Reward>();
		switch (room.RoomType)
		{
		case RoomType.Monster:
			rewards.Add(new GoldReward(room.Encounter.MinGoldReward, room.Encounter.MaxGoldReward, player));
			if (RollForPotionReward(player, room.RoomType))
			{
				rewards.Add(new PotionReward(player));
			}
			rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
			break;
		case RoomType.Elite:
			rewards.Add(new GoldReward(room.Encounter.MinGoldReward, room.Encounter.MaxGoldReward, player));
			if (RollForPotionReward(player, room.RoomType))
			{
				rewards.Add(new PotionReward(player));
			}
			rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
			rewards.Add(new RelicReward(player));
			break;
		case RoomType.Boss:
			rewards.Add(new GoldReward(room.Encounter.MinGoldReward, room.Encounter.MaxGoldReward, player));
			if (RollForPotionReward(player, room.RoomType))
			{
				rewards.Add(new PotionReward(player));
			}
			rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
			break;
		}

		foreach (Reward reward in rewards)
		{
			reward.Populate();
		}
		return rewards;
	}

	private static bool RollForPotionReward(Player player, RoomType roomType)
	{
		PotionRewardOdds odds = player.PlayerOdds.PotionReward;
		float currentValue = odds.CurrentValue;
		bool forced = player.Relics.Any((RelicModel relic) => relic.ShouldForcePotionReward(player, roomType));
		float roll = player.PlayerRng.Rewards.NextFloat();
		if (roll < currentValue || forced)
		{
			odds.OverrideCurrentValue(currentValue - 0.1f);
		}
		else
		{
			odds.OverrideCurrentValue(currentValue + 0.1f);
		}
		float eliteBonus = roomType == RoomType.Elite ? 0.25f : 0f;
		return forced || roll < currentValue + eliteBonus * 0.5f;
	}

	private static bool ShouldGenerateTreasure(Player player)
	{
		foreach (RelicModel relic in player.Relics)
		{
			if (!relic.ShouldGenerateTreasure(player))
			{
				return false;
			}
		}
		return true;
	}

	private static void ApplyRewardModifiers(Player player, List<Reward> rewards, AbstractRoom room)
	{
		List<RelicModel> modifiedRelics = new List<RelicModel>();
		foreach (RelicModel relic in player.Relics)
		{
			if (relic.TryModifyRewards(player, rewards, room))
			{
				modifiedRelics.Add(relic);
			}
		}
		foreach (RelicModel relic in player.Relics)
		{
			if (relic.TryModifyRewardsLate(player, rewards, room))
			{
				modifiedRelics.Add(relic);
			}
		}
		foreach (RelicModel relic in modifiedRelics.Distinct())
		{
			relic.AfterModifyingRewards();
		}
		foreach (Reward reward in rewards)
		{
			if (!reward.IsPopulated)
			{
				reward.Populate();
			}
		}
	}

	private static IEnumerable<myPendingRewardEntry> TranslateExtraRewards(Player player, myCombatSession session)
	{
		return TranslateRewards(player, session.ExtraRewards);
	}

	private static myPendingRewardEntry CloneAsNonSkippable(myPendingRewardEntry entry)
	{
		return new myPendingRewardEntry
		{
			Kind = entry.Kind,
			Label = entry.Label,
			GoldAmount = entry.GoldAmount,
			Potion = entry.Potion,
			Relic = entry.Relic,
			Cards = entry.Cards,
			CanSkip = false
		};
	}

	private static string BuildOfferSourceLabel(AbstractRoom? room)
	{
		if (room == null)
		{
			return "Custom rewards";
		}

		return room switch
		{
			CombatRoom combatRoom when combatRoom.Encounter != null => $"Combat rewards for {combatRoom.Encounter.Id.Entry}",
			TreasureRoom treasureRoom => $"Treasure rewards for act {treasureRoom.ActIndex + 1}",
			EventRoom eventRoom => $"Event rewards for {eventRoom.CanonicalEvent.Id}",
			_ => $"{room.RoomType} rewards"
		};
	}

	private static List<myPendingRewardEntry> TranslateRewards(Player player, IEnumerable<Reward> rewards)
	{
		List<myPendingRewardEntry> entries = new List<myPendingRewardEntry>();
		foreach (Reward reward in rewards ?? Array.Empty<Reward>())
		{
			if (!reward.IsPopulated)
			{
				reward.Populate();
			}

			switch (reward)
			{
			case GoldReward goldReward:
				entries.Add(new myPendingRewardEntry
				{
					Kind = myPendingRewardKind.Gold,
					Label = $"Gold x{goldReward.Amount}",
					GoldAmount = goldReward.Amount,
					CanSkip = false
				});
				break;
			case PotionReward potionReward:
			{
				PotionModel potion = potionReward.Potion ?? throw new InvalidOperationException("Potion reward is not populated.");
				entries.Add(new myPendingRewardEntry
				{
					Kind = myPendingRewardKind.Potion,
					Label = $"Potion {potion.Id.Entry}",
					Potion = potion,
					CanSkip = true
				});
				break;
			}
			case RelicReward relicReward:
			{
				RelicModel relic = ReadRelicFromReward(relicReward);
				entries.Add(new myPendingRewardEntry
				{
					Kind = myPendingRewardKind.Relic,
					Label = $"Relic {relic.Id.Entry}",
					Relic = relic,
					CanSkip = false
				});
				break;
			}
			case CardReward cardReward:
			{
				List<CardModel> cards = TranslateCardRewardOptions(player, cardReward);
				entries.Add(new myPendingRewardEntry
				{
					Kind = myPendingRewardKind.Card,
					Label = $"Card reward ({cards.Count} options)",
					Cards = cards,
					CanSkip = true
				});
				break;
			}
			case SpecialCardReward specialCardReward:
			{
				CardModel card = ReadSpecialCardFromReward(specialCardReward);
				entries.Add(new myPendingRewardEntry
				{
					Kind = myPendingRewardKind.SpecialCard,
					Label = $"Special card {card.Id.Entry}",
					Cards = new[] { card },
					CanSkip = false
				});
				break;
			}
			case CardRemovalReward:
				entries.Add(new myPendingRewardEntry
				{
					Kind = myPendingRewardKind.RemoveCard,
					Label = "Card removal",
					CanSkip = true
				});
				break;
			default:
				throw new NotSupportedException($"Unsupported reward type '{reward.GetType().Name}'.");
			}
		}
		return entries;
	}

	private static List<CardModel> TranslateCardRewardOptions(Player player, CardReward reward)
	{
		List<CardCreationResult> results = reward.Cards.Select((CardModel card) => new CardCreationResult(card)).ToList();
		if (TryReconstructCardCreationOptions(reward, out CardCreationOptions? creationOptions))
		{
			List<RelicModel> modifiedRelics = new List<RelicModel>();
			foreach (RelicModel relic in player.Relics)
			{
				if (relic.TryModifyCardRewardOptions(player, results, creationOptions))
				{
					modifiedRelics.Add(relic);
				}
			}
			foreach (RelicModel relic in player.Relics)
			{
				if (relic.TryModifyCardRewardOptionsLate(player, results, creationOptions))
				{
					modifiedRelics.Add(relic);
				}
			}
			foreach (RelicModel relic in modifiedRelics.Distinct())
			{
				relic.AfterModifyingCardRewardOptions();
			}
			return results.Select((CardCreationResult result) => result.Card).ToList();
		}

		List<CardModel> fallbackCards = reward.Cards.ToList();
		foreach (RelicModel relic in player.Relics)
		{
			relic.TryModifyCardRewardOptions(player, fallbackCards);
		}
		foreach (RelicModel relic in player.Relics)
		{
			relic.TryModifyCardRewardOptionsLate(player, fallbackCards);
		}
		return fallbackCards;
	}

	private static bool TryReconstructCardCreationOptions(CardReward reward, out CardCreationOptions? options)
	{
		try
		{
			SerializableReward save = reward.ToSerializable();
			options = new CardCreationOptions(save.CardPoolIds.Select(ModelDb.GetById<CardPoolModel>), save.Source, save.RarityOdds);
			return true;
		}
		catch
		{
			options = null;
			return false;
		}
	}

	private static RelicModel ReadRelicFromReward(RelicReward reward)
	{
		if (_relicRewardField?.GetValue(reward) is RelicModel relic)
		{
			return relic;
		}
		throw new InvalidOperationException("Could not extract relic payload from RelicReward.");
	}

	private static CardModel ReadSpecialCardFromReward(SpecialCardReward reward)
	{
		if (_specialCardRewardField?.GetValue(reward) is CardModel card)
		{
			return card;
		}
		throw new InvalidOperationException("Could not extract card payload from SpecialCardReward.");
	}

	private static void EnsureRelicBagsAreReady(Player player, RunState runState)
	{
		if (!runState.SharedRelicGrabBag.IsPopulated)
		{
			runState.SharedRelicGrabBag.Populate(ModelDb.RelicPool<SharedRelicPool>().GetUnlockedRelics(runState.UnlockState), runState.Rng.UpFront);
		}
		if (!player.RelicGrabBag.IsPopulated)
		{
			player.PopulateRelicGrabBagIfNecessary(runState.Rng.UpFront);
		}
	}

	private static void PrepareRewardCardForDeck(CardModel card, Player player)
	{
		if (!card.HasOwner)
		{
			card.Owner = player;
		}
	}
}
