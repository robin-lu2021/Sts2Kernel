using System;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Headless;

public readonly struct Color(string html)
{
	public string Html { get; } = html;
}

public struct Vector2
{
	public float X;

	public float Y;

	public static Vector2 Left => new Vector2(-1f, 0f);

	public Vector2(float x, float y)
	{
		X = x;
		Y = y;
	}

	public static Vector2 operator +(Vector2 left, Vector2 right)
	{
		return new Vector2(left.X + right.X, left.Y + right.Y);
	}

	public static Vector2 operator -(Vector2 left, Vector2 right)
	{
		return new Vector2(left.X - right.X, left.Y - right.Y);
	}

	public static Vector2 operator *(Vector2 left, float scalar)
	{
		return new Vector2(left.X * scalar, left.Y * scalar);
	}

	public static Vector2 operator *(float scalar, Vector2 right)
	{
		return right * scalar;
	}
}

public struct Vector2I
{
	public int X;

	public int Y;

	public Vector2I(int x, int y)
	{
		X = x;
		Y = y;
	}
}

public class Node2D
{
	public Vector2 Position { get; set; }

	public Vector2 GlobalPosition { get; set; }

	public Vector2 Scale { get; set; } = new Vector2(1f, 1f);

	public virtual void SetVisible(bool visible)
	{
	}

	public virtual void AddChildSafely(object child)
	{
	}
}

public sealed class NCreatureVisuals
{
	public Node2D Body { get; } = new Node2D();
}

public sealed class NOrbManager
{
	public void AddSlotAnim(int amount)
	{
	}

	public void RemoveSlotAnim(int amount)
	{
	}

	public void AddOrbAnim()
	{
	}

	public void EvokeOrbAnim(OrbModel orb)
	{
	}

	public void ReplaceOrb(OrbModel oldOrb, OrbModel newOrb)
	{
	}
}

public class NCreature : Node2D
{
	public NOrbManager OrbManager { get; } = new NOrbManager();

	public NCreatureVisuals Visuals { get; } = new NCreatureVisuals();

	public Vector2 VfxSpawnPosition { get; set; }

	public Vector2 Size { get; set; } = new Vector2(1f, 1f);

	public T? GetSpecialNode<T>(string path) where T : class, new()
	{
		return new T();
	}

	public Vector2 GetBottomOfHitbox()
	{
		return GlobalPosition;
	}

	public void ScaleTo(float scale, float duration)
	{
	}
}

public sealed class NCombatRoom
{
	public static NCombatRoom Instance { get; } = new NCombatRoom();

	public Node2D CombatVfxContainer { get; } = new Node2D();

	public NCreature GetCreatureNode(Creature creature)
	{
		return new NCreature();
	}

	public void AddCreature(Creature creature)
	{
	}
}

public sealed class NRun
{
	public static NRun Instance { get; } = new NRun();

	public HeadlessRunMusicController RunMusicController { get; } = new HeadlessRunMusicController();

	public HeadlessGlobalUi GlobalUi { get; } = new HeadlessGlobalUi();

	public void ShowGameOverScreen(SerializableRun serializableRun)
	{
	}
}

public sealed class HeadlessRunMusicController
{
	public void StopMusic()
	{
	}
}

public sealed class HeadlessGlobalUi
{
	public HeadlessTopBar TopBar { get; } = new HeadlessTopBar();
}

public sealed class HeadlessTopBar
{
	public HeadlessTopBarButton Map { get; } = new HeadlessTopBarButton();

	public HeadlessTopBarButton Deck { get; } = new HeadlessTopBarButton();
}

public sealed class HeadlessTopBarButton
{
	public void Enable()
	{
	}

	public void Disable()
	{
	}
}

public sealed class NHotkeyManager
{
	public static NHotkeyManager Instance { get; } = new NHotkeyManager();

	public void RemoveBlockingScreen(object? screen)
	{
	}
}

public sealed class NMerchantRoom
{
	public static NMerchantRoom Instance { get; } = new NMerchantRoom();

	public object Inventory { get; } = new object();

	public void UnblockInput()
	{
	}
}

public sealed class NMapScreen
{
	public static NMapScreen Instance { get; } = new NMapScreen();

	public bool Visible { get; set; }

	public void SetTravelEnabled(bool enabled)
	{
	}
}

public sealed class NDecimillipedeSegmentDriver
{
	public void AttackShake()
	{
	}
}

public sealed class NFireBurningVfx
{
	public static NFireBurningVfx Create(params object[] args)
	{
		return new NFireBurningVfx();
	}
}

public sealed class NFireBurstVfx
{
	public static NFireBurstVfx Create(params object[] args)
	{
		return new NFireBurstVfx();
	}
}

public sealed class NLargeMagicMissileVfx
{
	public static NLargeMagicMissileVfx Create(params object[] args)
	{
		return new NLargeMagicMissileVfx();
	}
}

public sealed class NGaseousImpactVfx
{
	public static NGaseousImpactVfx Create(params object[] args)
	{
		return new NGaseousImpactVfx();
	}
}

public sealed class NSnappingJaxfruitVfx
{
	public static NSnappingJaxfruitVfx Create(params object[] args)
	{
		return new NSnappingJaxfruitVfx();
	}
}
