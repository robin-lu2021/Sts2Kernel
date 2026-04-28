using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;

namespace MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;

public abstract class CrystalSphereItem
{
	public abstract (int X, int Y) Size { get; }

	public (int X, int Y) Position { get; private set; }

	protected virtual string TexturePath => ImageHelper.GetImagePath("events/crystal_sphere/" + GetType().Name.ToSnakeCase() + ".png");

	public abstract bool IsGood { get; }

	public event Action<CrystalSphereItem>? Revealed;

	public bool PlaceItem(CrystalSphereMinigame game)
	{
		List<(int X, int Y)> list = new List<(int X, int Y)>();
		for (int i = 0; i < game.GridSize.X; i++)
		{
			for (int j = 0; j < game.GridSize.Y; j++)
			{
				if (CanPlaceHere(game.cells, i, j))
				{
					list.Add((i, j));
				}
			}
		}
		if (!list.Any())
		{
			return false;
		}
		Position = game.Rng.NextItem(list);
		for (int k = 0; k < Size.X; k++)
		{
			for (int l = 0; l < Size.Y; l++)
			{
				int num = Position.X + k;
				int num2 = Position.Y + l;
				game.cells[num, num2].SetItem(this);
			}
		}
		return true;
	}

	private bool CanPlaceHere(CrystalSphereCell[,] grid, int x, int y)
	{
		for (int i = 0; i < Size.X; i++)
		{
			for (int j = 0; j < Size.Y; j++)
			{
				int num = x + i;
				int num2 = y + j;
				if (num < 0 || num >= grid.GetLength(0))
				{
					return false;
				}
				if (num2 < 0 || num2 >= grid.GetLength(1))
				{
					return false;
				}
				if (!grid[num, num2].IsHidden)
				{
					return false;
				}
				if (grid[num, num2].Item != null)
				{
					return false;
				}
			}
		}
		return true;
	}

	public virtual Task RevealItem(Player _)
	{
		this.Revealed?.Invoke(this);
		return Task.CompletedTask;
	}
}
