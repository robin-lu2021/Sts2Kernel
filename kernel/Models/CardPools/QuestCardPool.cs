using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.CardPools;

public sealed class QuestCardPool : CardPoolModel
{
	public override string Title => "quest";

	public override string EnergyColorName => "colorless";

	public override bool IsColorless => false;

	protected override CardModel[] GenerateAllCards()
	{
		return new CardModel[3]
		{
			ModelDb.Card<ByrdonisEgg>(),
			ModelDb.Card<LanternKey>(),
			ModelDb.Card<SpoilsMap>()
		};
	}
}
