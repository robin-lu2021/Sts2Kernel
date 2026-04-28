using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models;

public abstract class CharacterModel : AbstractModel
{
	public const string locTable = "characters";

	protected const string _relaxedTrigger = "Relaxed";

	protected const string _idleTrigger = "Idle";

	public const string relaxedAnim = "relaxed_loop";

	public LocString Title => new LocString("characters", base.Id.Entry + ".title");

	public LocString TitleObject => new LocString("characters", base.Id.Entry + ".titleObject");

	public abstract CharacterGender Gender { get; }

	protected abstract CharacterModel? UnlocksAfterRunAs { get; }

	public LocString PronounObject => new LocString("characters", base.Id.Entry + ".pronounObject");

	public LocString PossessiveAdjective => new LocString("characters", base.Id.Entry + ".possessiveAdjective");

	public LocString PronounPossessive => new LocString("characters", base.Id.Entry + ".pronounPossessive");

	public LocString PronounSubject => new LocString("characters", base.Id.Entry + ".pronounSubject");

	public LocString CardsModifierTitle => new LocString("characters", base.Id.Entry + ".cardsModifierTitle");

	public LocString CardsModifierDescription => new LocString("characters", base.Id.Entry + ".cardsModifierDescription");

	public LocString EventDeathPreventionLine => new LocString("characters", base.Id.Entry + ".eventDeathPrevention");

	public string TrailPath => SceneHelper.GetScenePath("vfx/card_trail_" + base.Id.Entry.ToLowerInvariant());

	public abstract int StartingHp { get; }

	public abstract int StartingGold { get; }

	public virtual int MaxEnergy => 3;

	public virtual int BaseOrbSlotCount => 0;

	public virtual bool ShouldAlwaysShowStarCounter => false;

	public abstract CardPoolModel CardPool { get; }

	public abstract RelicPoolModel RelicPool { get; }

	public abstract PotionPoolModel PotionPool { get; }

	public abstract IEnumerable<CardModel> StartingDeck { get; }

	public abstract IReadOnlyList<RelicModel> StartingRelics { get; }

	public virtual IReadOnlyList<PotionModel> StartingPotions => Array.Empty<PotionModel>();

	private string VisualsPath => SceneHelper.GetScenePath("creature_visuals/" + base.Id.Entry.ToLowerInvariant());

	private string IconTexturePath => ImageHelper.GetImagePath("ui/top_panel/character_icon_" + base.Id.Entry.ToLowerInvariant() + ".png");

	private string IconPath => SceneHelper.GetScenePath("ui/character_icons/" + base.Id.Entry.ToLowerInvariant() + "_icon");

	public string EnergyCounterPath => SceneHelper.GetScenePath("combat/energy_counters/" + base.Id.Entry.ToLowerInvariant() + "_energy_counter");

	public string MerchantAnimPath => SceneHelper.GetScenePath("merchant/characters/" + base.Id.Entry.ToLowerInvariant() + "_merchant");

	public string RestSiteAnimPath => SceneHelper.GetScenePath("rest_site/characters/" + base.Id.Entry.ToLowerInvariant() + "_rest_site");

	public Achievement RunWonAchievement => Enum.Parse<Achievement>(base.Id.Entry.Capitalize() + "Win");

	protected virtual IEnumerable<string> ExtraAssetPaths => Array.Empty<string>();

	public string CharacterSelectTitle => base.Id.Entry.ToUpperInvariant() + ".title";

	public string CharacterSelectDesc => base.Id.Entry.ToUpperInvariant() + ".description";

	public string CharacterSelectBg => SceneHelper.GetScenePath("screens/char_select/char_select_bg_" + base.Id.Entry.ToLowerInvariant());

	protected virtual string CharacterSelectIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_" + base.Id.Entry.ToLowerInvariant() + ".png");

	protected virtual string CharacterSelectLockedIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_" + base.Id.Entry.ToLowerInvariant() + "_locked.png");

	public string CharacterSelectTransitionPath => "res://materials/transitions/" + base.Id.Entry.ToLowerInvariant() + "_transition_mat.tres";

	protected virtual string MapMarkerPath => ImageHelper.GetImagePath("packed/map/icons/map_marker_" + base.Id.Entry.ToLowerInvariant() + ".png");

	public IEnumerable<string> AssetPathsCharacterSelect => new global::_003C_003Ez__ReadOnlyArray<string>(new string[5] { CharacterSelectBg, CharacterSelectIconPath, IconTexturePath, CharacterSelectLockedIconPath, CharacterSelectTransitionPath });

	public IEnumerable<string> AssetPaths => new string[9] { VisualsPath, IconTexturePath, IconPath, EnergyCounterPath, RestSiteAnimPath, MerchantAnimPath, CharacterSelectTransitionPath, MapMarkerPath, TrailPath }.Concat(ExtraAssetPaths);

	public abstract float AttackAnimDelay { get; }

	public abstract float CastAnimDelay { get; }

	public virtual string CharacterSelectSfx => $"event:/sfx/characters/{base.Id.Entry.ToLowerInvariant()}/{base.Id.Entry.ToLowerInvariant()}_select";

	public string AttackSfx => $"event:/sfx/characters/{base.Id.Entry.ToLowerInvariant()}/{base.Id.Entry.ToLowerInvariant()}_attack";

	public string CastSfx => $"event:/sfx/characters/{base.Id.Entry.ToLowerInvariant()}/{base.Id.Entry.ToLowerInvariant()}_cast";

	public string DeathSfx => $"event:/sfx/characters/{base.Id.Entry.ToLowerInvariant()}/{base.Id.Entry.ToLowerInvariant()}_die";

	public virtual string CharacterTransitionSfx => "event:/sfx/ui/wipe_" + base.Id.Entry.ToLowerInvariant();

	public override bool ShouldReceiveCombatHooks => false;

	public abstract List<string> GetArchitectAttackVfx();

	public void AddDetailsTo(LocString str)
	{
		str.Add("character", Title);
		str.Add("characterObject", TitleObject);
		str.Add("characterGender", Gender.ToString().ToLowerInvariant());
		str.Add("possessiveAdjective", PossessiveAdjective);
		str.Add("pronounObject", PronounObject);
		str.Add("pronounPossessive", PronounPossessive);
		str.Add("pronounSubject", PronounSubject);
	}

	public LocString GetUnlockText()
	{
		LocString locString = new LocString("characters", base.Id.Entry + ".unlockText");
		LocString locString2 = new LocString("characters", "LOCKED.title");
		LocString variable;
		if (UnlocksAfterRunAs == null)
		{
			variable = locString2;
		}
		else
		{
			UnlockState unlockState = SaveManager.Instance.GenerateUnlockStateFromProgress();
			variable = ((!unlockState.Characters.Contains<CharacterModel>(UnlocksAfterRunAs)) ? locString2 : UnlocksAfterRunAs.Title);
		}
		locString.Add("Prerequisite", variable);
		return locString;
	}
}
