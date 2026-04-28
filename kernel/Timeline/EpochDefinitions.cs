using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Potions;

namespace MegaCrit.Sts2.Core.Timeline.Epochs;

public sealed class Act2BEpoch : EpochModel
{
}

public sealed class Act3BEpoch : EpochModel
{
}

public sealed class Colorless1Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Automation>(),
		ModelDb.Card<Entropy>(),
		ModelDb.Card<Catastrophe>()
	};
}

public sealed class Colorless2Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<EternalArmor>(),
		ModelDb.Card<Jackpot>(),
		ModelDb.Card<PrepTime>()
	};
}

public sealed class Colorless3Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Rend>(),
		ModelDb.Card<BeatDown>(),
		ModelDb.Card<Prowess>()
	};
}

public sealed class Colorless4Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Alchemize>(),
		ModelDb.Card<Nostalgia>(),
		ModelDb.Card<Scrawl>()
	};
}

public sealed class Colorless5Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Splash>(),
		ModelDb.Card<Anointed>(),
		ModelDb.Card<Calamity>()
	};
}

public sealed class CustomAndSeedsEpoch : EpochModel
{
}

public sealed class DailyRunEpoch : EpochModel
{
}

public sealed class DarvEpoch : EpochModel
{
}

public sealed class Defect1Epoch : EpochModel
{
}

public sealed class Defect2Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Loop>(),
		ModelDb.Card<Null>(),
		ModelDb.Card<ConsumingShadow>()
	};
}

public sealed class Defect3Epoch : EpochModel
{
}

public sealed class Defect4Epoch : EpochModel
{
	public static List<PotionModel> Potions => new List<PotionModel>
	{
		ModelDb.Potion<FocusPotion>(),
		ModelDb.Potion<EssenceOfDarkness>(),
		ModelDb.Potion<PotionOfCapacity>()
	};
}

public sealed class Defect5Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Barrage>(),
		ModelDb.Card<FlakCannon>(),
		ModelDb.Card<Smokestack>()
	};
}

public sealed class Defect6Epoch : EpochModel
{
}

public sealed class Defect7Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Turbo>(),
		ModelDb.Card<Scavenge>(),
		ModelDb.Card<HelixDrill>()
	};
}

public sealed class Event1Epoch : EpochModel
{
	public static List<EventModel> Events => new List<EventModel> { ModelDb.Event<TrashHeap>() };
}

public sealed class Event2Epoch : EpochModel
{
	public static List<EventModel> Events => new List<EventModel> { ModelDb.Event<Reflections>() };
}

public sealed class Event3Epoch : EpochModel
{
	public static List<EventModel> Events => new List<EventModel> { ModelDb.Event<ColorfulPhilosophers>() };
}

public sealed class Ironclad2Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<MoltenFist>(),
		ModelDb.Card<Cruelty>(),
		ModelDb.Card<Dominate>()
	};
}

public sealed class Ironclad3Epoch : EpochModel
{
}

public sealed class Ironclad4Epoch : EpochModel
{
	public static List<PotionModel> Potions => new List<PotionModel>
	{
		ModelDb.Potion<BloodPotion>(),
		ModelDb.Potion<SoldiersStew>(),
		ModelDb.Potion<Ashwater>()
	};
}

public sealed class Ironclad5Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Cinder>(),
		ModelDb.Card<PactsEnd>(),
		ModelDb.Card<DrumOfBattle>()
	};
}

public sealed class Ironclad6Epoch : EpochModel
{
}

public sealed class Ironclad7Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<BloodWall>(),
		ModelDb.Card<TearAsunder>(),
		ModelDb.Card<Inferno>()
	};
}

public sealed class Necrobinder1Epoch : EpochModel
{
}

public sealed class Necrobinder2Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Scourge>(),
		ModelDb.Card<Oblivion>(),
		ModelDb.Card<Countdown>()
	};
}

public sealed class Necrobinder3Epoch : EpochModel
{
}

public sealed class Necrobinder4Epoch : EpochModel
{
	public static List<PotionModel> Potions => new List<PotionModel>
	{
		ModelDb.Potion<PotionOfDoom>(),
		ModelDb.Potion<PotOfGhouls>(),
		ModelDb.Potion<BoneBrew>()
	};
}

public sealed class Necrobinder5Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Afterlife>(),
		ModelDb.Card<Sacrifice>(),
		ModelDb.Card<Calcify>()
	};
}

public sealed class Necrobinder6Epoch : EpochModel
{
}

public sealed class Necrobinder7Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<SculptingStrike>(),
		ModelDb.Card<Veilpiercer>(),
		ModelDb.Card<BansheesCry>()
	};
}

public sealed class NeowEpoch : EpochModel
{
}

public sealed class OrobasEpoch : EpochModel
{
}

public sealed class Potion1Epoch : EpochModel
{
	public static List<PotionModel> Potions => new List<PotionModel>
	{
		ModelDb.Potion<BeetleJuice>(),
		ModelDb.Potion<MazalethsGift>(),
		ModelDb.Potion<DropletOfPrecognition>()
	};
}

public sealed class Potion2Epoch : EpochModel
{
	public static List<PotionModel> Potions => new List<PotionModel>
	{
		ModelDb.Potion<PowderedDemise>(),
		ModelDb.Potion<ShipInABottle>(),
		ModelDb.Potion<TouchOfInsanity>()
	};
}

public sealed class Regent1Epoch : EpochModel
{
}

public sealed class Regent2Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<SpoilsOfBattle>(),
		ModelDb.Card<SwordSage>(),
		ModelDb.Card<Furnace>()
	};
}

public sealed class Regent3Epoch : EpochModel
{
}

public sealed class Regent4Epoch : EpochModel
{
	public static List<PotionModel> Potions => new List<PotionModel>
	{
		ModelDb.Potion<StarPotion>(),
		ModelDb.Potion<CosmicConcoction>(),
		ModelDb.Potion<KingsCourage>()
	};
}

public sealed class Regent5Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Begone>(),
		ModelDb.Card<Arsenal>(),
		ModelDb.Card<Supermassive>()
	};
}

public sealed class Regent6Epoch : EpochModel
{
}

public sealed class Regent7Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Patter>(),
		ModelDb.Card<HeavenlyDrill>(),
		ModelDb.Card<LunarBlast>()
	};
}

public sealed class Relic1Epoch : EpochModel
{
}

public sealed class Relic2Epoch : EpochModel
{
}

public sealed class Relic3Epoch : EpochModel
{
}

public sealed class Relic4Epoch : EpochModel
{
}

public sealed class Relic5Epoch : EpochModel
{
}

public sealed class Silent1Epoch : EpochModel
{
}

public sealed class Silent2Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Snakebite>(),
		ModelDb.Card<BubbleBubble>(),
		ModelDb.Card<Accelerant>()
	};
}

public sealed class Silent3Epoch : EpochModel
{
}

public sealed class Silent4Epoch : EpochModel
{
	public static List<PotionModel> Potions => new List<PotionModel>
	{
		ModelDb.Potion<PoisonPotion>(),
		ModelDb.Potion<GhostInAJar>(),
		ModelDb.Potion<CunningPotion>()
	};
}

public sealed class Silent5Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<Reflex>(),
		ModelDb.Card<MasterPlanner>(),
		ModelDb.Card<HandTrick>()
	};
}

public sealed class Silent6Epoch : EpochModel
{
}

public sealed class Silent7Epoch : EpochModel
{
	public static List<CardModel> Cards => new List<CardModel>
	{
		ModelDb.Card<HiddenDaggers>(),
		ModelDb.Card<BladeOfInk>(),
		ModelDb.Card<PhantomBlades>()
	};
}

public sealed class UnderdocksEpoch : EpochModel
{
}
