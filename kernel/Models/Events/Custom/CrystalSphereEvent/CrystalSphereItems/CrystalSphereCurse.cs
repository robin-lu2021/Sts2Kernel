using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent.CrystalSphereItems;

public class CrystalSphereCurse : CrystalSphereItem
{
	public override (int X, int Y) Size => (2, 2);

	public override bool IsGood => false;

	public override async Task RevealItem(Player owner)
	{
		await base.RevealItem(owner);
		CardPileCmd.AddCurseToDeck<Doubt>(owner);
	}
}
