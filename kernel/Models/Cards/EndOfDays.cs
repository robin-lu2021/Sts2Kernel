using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class EndOfDays : CardModel
{
	public const int doomAmount = 29;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new PowerVar<DoomPower>(29m));

	public EndOfDays()
		: base(3, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (Creature hittableEnemy in base.CombatState.HittableEnemies)
		{
			PowerCmd.Apply<DoomPower>(hittableEnemy, base.DynamicVars.Doom.BaseValue, base.Owner.Creature, this);
		}
		DoomPower.DoomKill(DoomPower.GetDoomedCreatures(base.CombatState.HittableEnemies));
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Doom.UpgradeValueBy(8m);
	}
}
