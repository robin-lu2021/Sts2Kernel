using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Models;

public abstract class OrbModel : AbstractModel
{
	public const string locTable = "orbs";

	private static readonly ModelId[] _validOrbs = new ModelId[5]
	{
		ModelDb.GetId<LightningOrb>(),
		ModelDb.GetId<FrostOrb>(),
		ModelDb.GetId<DarkOrb>(),
		ModelDb.GetId<PlasmaOrb>(),
		ModelDb.GetId<GlassOrb>()
	};

	private OrbModel _canonicalInstance;

	private Player? _owner;

	public abstract decimal PassiveVal { get; }

	public abstract decimal EvokeVal { get; }

	public bool HasBeenRemovedFromState { get; private set; }

	public LocString Title => new LocString("orbs", base.Id.Entry + ".title");

	public LocString Description => new LocString("orbs", base.Id.Entry + ".description");

	public bool HasSmartDescription => LocString.Exists("orbs", SmartDescriptionLocKey);

	private string SmartDescriptionLocKey => base.Id.Entry + ".smartDescription";

	public LocString SmartDescription
	{
		get
		{
			if (!HasSmartDescription)
			{
				return Description;
			}
			return new LocString("orbs", base.Id.Entry + ".smartDescription");
		}
	}

	private string DebugPassiveSfx => base.Id.Entry.ToLowerInvariant() + "_passive.mp3";

	private string DebugEvokeSfx => base.Id.Entry.ToLowerInvariant() + "_evoke.mp3";

	private string DebugChannelSfx => base.Id.Entry.ToLowerInvariant() + "_channel.mp3";

	protected virtual string PassiveSfx => "";

	protected virtual string EvokeSfx => "";

	protected virtual string ChannelSfx => "";

	public static HoverTip EmptySlotHoverTipHoverTip => new HoverTip(new LocString("orbs", "EMPTY_SLOT.title"), new LocString("orbs", "EMPTY_SLOT.description"));

	public HoverTip DumbHoverTip => new HoverTip(this, Description);

	protected virtual IEnumerable<IHoverTip> ExtraHoverTips => Array.Empty<IHoverTip>();

	public IEnumerable<IHoverTip> HoverTips
	{
		get
		{
			List<IHoverTip> list = ExtraHoverTips.ToList();
			if (HasSmartDescription && base.IsMutable)
			{
				LocString smartDescription = SmartDescription;
				smartDescription.Add("energyPrefix", Owner.Character.CardPool.Title);
				smartDescription.Add("Passive", PassiveVal);
				smartDescription.Add("Evoke", EvokeVal);
				list.Add(new HoverTip(this, smartDescription));
			}
			else
			{
				list.Add(DumbHoverTip);
			}
			return list;
		}
	}

	private string IconPath => ImageHelper.GetImagePath("orbs/" + base.Id.Entry.ToLowerInvariant() + ".png");

	public string Icon => IconPath;

	private OrbModel CanonicalInstance
	{
		get
		{
			if (!base.IsMutable)
			{
				return this;
			}
			return _canonicalInstance;
		}
		set
		{
			AssertMutable();
			_canonicalInstance = value;
		}
	}

	public Player Owner
	{
		get
		{
			AssertMutable();
			return _owner;
		}
		set
		{
			AssertMutable();
			if (_owner != null && value != null && value != _owner)
			{
				throw new InvalidOperationException("Card " + base.Id.Entry + " already has an owner.");
			}
			_owner = value;
		}
	}

	public CombatState CombatState => Owner.Creature.CombatState;

	public override bool ShouldReceiveCombatHooks => true;

	public IEnumerable<string> AssetPaths => new global::_003C_003Ez__ReadOnlySingleElementList<string>(IconPath);

	public event Action? Triggered;

	public static OrbModel GetRandomOrb(Rng rng)
	{
		return ModelDb.GetById<OrbModel>(rng.NextItem(_validOrbs));
	}

	public OrbModel ToMutable(int initialAmount = 0)
	{
		AssertCanonical();
		OrbModel orbModel = (OrbModel)MutableClone();
		orbModel.CanonicalInstance = this;
		return orbModel;
	}

	public void Trigger()
	{
		this.Triggered?.Invoke();
	}

	public virtual Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
	{
		return Task.CompletedTask;
	}

	public virtual Task Passive(PlayerChoiceContext choiceContext, Creature? target)
	{
		return Task.CompletedTask;
	}

	public virtual Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
	{
		return Task.FromResult((IEnumerable<Creature>)Array.Empty<Creature>());
	}

	protected decimal ModifyOrbValue(decimal result)
	{
		return Hook.ModifyOrbValue(Owner.Creature.CombatState, Owner, result);
	}

	protected override void AfterCloned()
	{
		base.AfterCloned();
		this.Triggered = null;
	}

	public void RemoveInternal()
	{
		HasBeenRemovedFromState = true;
	}

	public virtual void PlayPassiveSfx()
	{
	}

	public virtual void PlayEvokeSfx()
	{
	}

	public virtual void PlayChannelSfx()
	{
	}
}
