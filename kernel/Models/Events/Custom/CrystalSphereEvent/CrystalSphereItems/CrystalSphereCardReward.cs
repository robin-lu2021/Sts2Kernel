using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent.CrystalSphereItems;

public class CrystalSphereCardReward : CrystalSphereItem
{
	private readonly Player _owner;

	private readonly CrystalSphereMinigame _grid;

	private readonly CardRarity _rarity;

	public override (int X, int Y) Size => (2, 2);

	protected override string TexturePath => ImageHelper.GetImagePath("events/crystal_sphere/crystal_sphere_" + _rarity.ToString().ToLowerInvariant() + "_card_reward.png");

	public override bool IsGood => true;

	public CrystalSphereCardReward(CrystalSphereMinigame grid, CardRarity rarity, Player owner)
	{
		_grid = grid;
		_rarity = rarity;
		_owner = owner;
	}

	public override async Task RevealItem(Player owner)
	{
		await base.RevealItem(owner);
		CardCreationOptions options = new CardCreationOptions(new global::_003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(owner.Character.CardPool), CardCreationSource.Other, CardRarityOddsType.Uniform, (CardModel c) => c.Rarity == _rarity).WithRngOverride(_grid.Rng);
		_grid.AddReward(new CardReward(options, 3, owner).SetRng(_grid.Rng));
	}
}
