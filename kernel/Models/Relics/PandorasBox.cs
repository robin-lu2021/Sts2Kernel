using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class PandorasBox : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Ancient;

	public override bool HasUponPickupEffect => true;

	public override void AfterObtained()
	{
		List<CardModel> source = PileType.Deck.GetPile(base.Owner).Cards.Where((CardModel c) => c != null && c.IsBasicStrikeOrDefend && c.IsRemovable).ToList();
		IEnumerable<CardTransformation> transformations = source.Select((CardModel c) => new CardTransformation(c, CardFactory.CreateRandomCardForTransform(c, isInCombat: false, base.Owner.RunState.Rng.Niche)));
		_ = CardCmd.Transform(transformations, null, CardPreviewStyle.None).ToList();
	}
}
