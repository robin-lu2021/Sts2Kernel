using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core;

public abstract class PotionModel : AbstractModel
{
	public const string locTable = "potions";

	private Player? _owner;

	private DynamicVarSet? _dynamicVars;

	private PotionModel _canonicalInstance;

	public virtual string ContentId => Id.Entry;

	public LocString Title => new LocString(locTable, ContentId + ".title");

	private LocString Description => new LocString("potions", base.Id.Entry + ".description");

	public LocString SelectionScreenPrompt => new LocString(locTable, ContentId + ".selectionScreenPrompt");

	public LocString DynamicDescription
	{
		get
		{
			LocString description = Description;
			DynamicVars.AddTo(description);
			return description;
		}
	}

	public abstract PotionRarity Rarity { get; }

	public abstract PotionUsage Usage { get; }

	public abstract TargetType TargetType { get; }

	public PotionPoolModel Pool => ModelDb.AllPotionPools.First((PotionPoolModel p) => p.AllPotionIds.Contains(Id));

	public Player Owner
	{
		get
		{
			AssertMutable();
			return _owner ?? throw new InvalidOperationException($"Potion '{ContentId}' does not have an owner yet.");
		}
		set
		{
			AssertMutable();
			if (_owner != null && !ReferenceEquals(_owner, value))
			{
				throw new InvalidOperationException("Cannot move potion " + Id.Entry + " from one owner to another");
			}
			_owner = value;
		}
	}

	public DynamicVarSet DynamicVars
	{
		get
		{
			if (_dynamicVars != null)
			{
				return _dynamicVars;
			}
			_dynamicVars = new DynamicVarSet(CanonicalVars);
			_dynamicVars.InitializeWithOwner(this);
			return _dynamicVars;
		}
	}

	protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

	public bool IsQueued { get; private set; }

	public virtual bool CanBeGeneratedInCombat => true;

	public virtual bool PassesCustomUsabilityCheck => true;

	public virtual HoverTip HoverTip
	{
		get
		{
			HoverTip result = new HoverTip(Title, DynamicDescription);
			result.SetCanonicalModel(CanonicalInstance);
			return result;
		}
	}

	public IEnumerable<IHoverTip> HoverTips => new IHoverTip[1] { HoverTip }.Concat(ExtraHoverTips);

	public virtual IEnumerable<IHoverTip> ExtraHoverTips => Array.Empty<IHoverTip>();

	public PotionModel CanonicalInstance
	{
		get
		{
			if (!IsMutable)
			{
				return this;
			}
			return _canonicalInstance ?? this;
		}
		private set
		{
			AssertMutable();
			_canonicalInstance = value;
		}
	}

	public override bool ShouldReceiveCombatHooks => true;

	public bool HasBeenRemovedFromState { get; private set; }

	public event Action? BeforeUse;

	public PotionModel ToMutable()
	{
		AssertCanonical();
		PotionModel clone = (PotionModel)MutableClone();
		clone.CanonicalInstance = this;
		return clone;
	}

	protected override void AfterCloned()
	{
		base.AfterCloned();
		HasBeenRemovedFromState = false;
		IsQueued = false;
		_owner = null;
		BeforeUse = null;
	}

	public void Discard()
	{
		Owner.DiscardPotionInternal(this);
		HasBeenRemovedFromState = true;
	}

	public void RemoveBeforeUse()
	{
		Owner.RemoveUsedPotionInternal(this);
		HasBeenRemovedFromState = true;
	}

	public void EnqueueManualUse(Creature? target)
	{
		AssertMutable();
		BeforeUse?.Invoke();
		UsePotionAction action = new UsePotionAction(this, target, CombatManager.Instance.IsInProgress);
		IsQueued = true;
		RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(action);
	}

	public void Use(Creature? target = null)
	{
		Use(choiceContext: null, target);
	}

	public void Use(PlayerChoiceContext? choiceContext, Creature? target)
	{
		OnUseWrapper(choiceContext ?? new ThrowingPlayerChoiceContext(), target);
	}

	public virtual void OnUseWrapper(PlayerChoiceContext choiceContext, Creature? target)
	{
		RemoveBeforeUse();
		CombatState? combatState = Owner.Creature.CombatState;
		choiceContext.PushModel(this);
		Hook.BeforePotionUsed(Owner.RunState, combatState, this, target);
		OnUse(choiceContext, target);
		InvokeExecutionFinished();
		if (combatState != null && CombatManager.Instance.IsInProgress)
		{
			CombatManager.Instance.History.PotionUsed(combatState, this, target);
		}
		Hook.AfterPotionUsed(Owner.RunState, combatState, this, target);
		Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(Owner.NetId).PotionUsed.Add(Id);
		CombatManager.Instance.CheckForEmptyHand(choiceContext, Owner);
		choiceContext.PopModel(this);
	}

	public void AfterUsageCanceled()
	{
		IsQueued = false;
	}

	protected virtual void OnUse(PlayerChoiceContext? choiceContext, Creature? target)
	{
	}

	public mySerializablePotion SaveState(int slotIndex = -1)
	{
		AssertMutable();
		mySerializablePotion save = new mySerializablePotion
		{
			Id = ContentId,
			SlotIndex = slotIndex
		};
		WriteCustomState(save.State);
		return save;
	}

	public mySerializablePotion ToSerializable(int slotIndex)
	{
		return SaveState(slotIndex);
	}

	public virtual void LoadState(mySerializablePotion save)
	{
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		if (!string.IsNullOrWhiteSpace(save.Id) && !string.Equals(save.Id, ContentId, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException($"Cannot load potion state for '{save.Id}' into '{ContentId}'.");
		}
		ReadCustomState(save.State);
	}

	public static PotionModel FromSerializable(mySerializablePotion save)
	{
		if (save == null)
		{
			throw new ArgumentNullException(nameof(save));
		}
		PotionModel potion = TryCreateKernelEquivalent(save.Id) ?? SaveUtil.PotionOrDeprecated(new ModelId("POTION", save.Id)).ToMutable();
		potion.LoadState(save);
		return potion;
	}

	public static PotionModel FromSerializable(SerializablePotion save)
	{
		return FromSerializable((mySerializablePotion)save);
	}

	public static PotionModel FromCore(PotionModel sourcePotion)
	{
		if (sourcePotion == null)
		{
			throw new ArgumentNullException(nameof(sourcePotion));
		}
		PotionModel potion = TryCreateKernelEquivalent(sourcePotion.ContentId) ?? SaveUtil.PotionOrDeprecated(new ModelId("POTION", sourcePotion.ContentId)).ToMutable();
		potion.LoadState(sourcePotion.SaveState());
		return potion;
	}

	protected virtual void WriteCustomState(Dictionary<string, string> state)
	{
	}

	protected virtual void ReadCustomState(IReadOnlyDictionary<string, string> state)
	{
	}

	protected static void AssertValidForTargetedPotion(Creature? target)
	{
		if (target == null)
		{
			throw new ArgumentNullException(nameof(target), "Target must be present for targeted potions.");
		}
	}

	public bool CanThrowAtAlly()
	{
		if (TargetType == TargetType.AnyPlayer && Owner.RunState.Players.Count > 1)
		{
			return CombatManager.Instance.IsInProgress;
		}
		return false;
	}

	private static PotionModel? TryCreateKernelEquivalent(string potionId)
	{
		foreach (PotionModel canonical in ModelDb.AllPotions)
		{
			if (canonical.Id.Entry.Equals(potionId, StringComparison.OrdinalIgnoreCase))
			{
				return canonical.ToMutable();
			}
		}
		return null;
	}
}

public sealed class mySerializablePotion : IPacketSerializable
{
	public string Id { get; set; } = string.Empty;

	public int SlotIndex { get; set; } = -1;

	public Dictionary<string, string> State { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

	public SerializablePotion ToRunSerializable()
	{
		return new SerializablePotion
		{
			Id = string.IsNullOrWhiteSpace(Id) ? null : new ModelId("POTION", Id),
			SlotIndex = SlotIndex
		};
	}

	public static implicit operator SerializablePotion(mySerializablePotion save)
	{
		return save?.ToRunSerializable() ?? new SerializablePotion();
	}

	public static implicit operator mySerializablePotion(SerializablePotion save)
	{
		return new mySerializablePotion
		{
			Id = save?.Id?.Entry ?? string.Empty,
			SlotIndex = save?.SlotIndex ?? -1
		};
	}

	public void Serialize(PacketWriter writer)
	{
		writer.WriteString(Id);
		writer.WriteInt(SlotIndex);
		writer.WriteInt(State.Count);
		foreach (KeyValuePair<string, string> pair in State.OrderBy((KeyValuePair<string, string> kvp) => kvp.Key, StringComparer.Ordinal))
		{
			writer.WriteString(pair.Key);
			writer.WriteString(pair.Value);
		}
	}

	public void Deserialize(PacketReader reader)
	{
		Id = reader.ReadString();
		SlotIndex = reader.ReadInt();
		int count = reader.ReadInt();
		State = new Dictionary<string, string>(count, StringComparer.Ordinal);
		for (int i = 0; i < count; i++)
		{
			State[reader.ReadString()] = reader.ReadString();
		}
	}

	private static PotionModel? TryCreateKernelEquivalent(string potionId)
	{
		foreach (PotionModel canonical in ModelDb.AllPotions)
		{
			if (canonical.Id.Entry.Equals(potionId, StringComparison.OrdinalIgnoreCase))
			{
				return canonical.ToMutable();
			}
		}
		return null;
	}
}
