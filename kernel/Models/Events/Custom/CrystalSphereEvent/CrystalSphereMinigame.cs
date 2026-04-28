using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent.CrystalSphereItems;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;

namespace MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;

public class CrystalSphereMinigame
{
	public enum CrystalSphereToolType
	{
		None,
		Small,
		Big
	}

	private const int _defaultWidth = 11;

	private const int _defaultHeight = 11;

	private int _divinationCount;

	private readonly TaskCompletionSource _completionSource = new TaskCompletionSource();

	private readonly Player _owner;

	public CrystalSphereCell[,] cells;

	private readonly List<CrystalSphereItem> _items = new List<CrystalSphereItem>();

	private readonly List<Reward> _rewards = new List<Reward>();

	public Rng Rng { get; private set; }

	public int DivinationCount
	{
		get
		{
			return _divinationCount;
		}
		set
		{
			_divinationCount = value;
			this.DivinationCountChanged?.Invoke();
		}
	}

	public (int X, int Y) GridSize => (cells.GetLength(0), cells.GetLength(1));

	public bool IsFinished => DivinationCount == 0;

	public bool PlacedAllItems { get; private set; }

	public CrystalSphereToolType CrystalSphereTool { get; private set; }

	public IReadOnlyList<CrystalSphereItem> Items => _items;

	private CrystalSphereCell? HoveredCell { get; set; }

	private List<CrystalSphereCell> HighlightedCells { get; set; } = new List<CrystalSphereCell>();

	public event Action? DivinationCountChanged;

	public event Action? Finished;

	public CrystalSphereMinigame(Player owner, Rng rng, int divinationCount)
	{
		_owner = owner;
		Rng = rng;
		cells = new CrystalSphereCell[11, 11];
		for (int i = 0; i < 11; i++)
		{
			for (int j = 0; j < 11; j++)
			{
				cells[i, j] = new CrystalSphereCell(i, j);
			}
		}
		List<(int X, int Y)> list2 = new List<(int X, int Y)>
		{
			(0, 0),
			(cells.GetLength(0) - 1, 0),
			(cells.GetLength(0) - 1, cells.GetLength(0) - 1),
			(0, cells.GetLength(0) - 1)
		};
		for (int k = 0; k < 2; k++)
		{
			List<(int X, int Y)> list3 = list2.Concat(list2.SelectMany(c => GetHorizontalCells(c.X, c.Y))).Concat(list2.SelectMany(c => GetVerticalCells(c.X, c.Y))).ToList();
			list2 = list3;
		}
		foreach ((int X, int Y) item in list2)
		{
			TaskHelper.RunSafely(ClearCell(item.X, item.Y));
		}
		int num3 = 0;
		do
		{
			PlacedAllItems = PopulateItems();
			num3++;
		}
		while (!PlacedAllItems && num3 < 10);
		DivinationCount = divinationCount;
		CrystalSphereTool = CrystalSphereToolType.Big;
	}

	public void ForceMinigameEnd()
	{
		_rewards.Clear();
		if (!_completionSource.Task.IsCompleted)
		{
			_completionSource.SetCanceled();
		}
	}

	public Task PlayMinigame()
	{
		return Task.CompletedTask;
	}

	private bool PopulateItems()
	{
		bool flag = true;
		CrystalSphereItem crystalSphereItem = new CrystalSphereRelic(this);
		flag = flag && crystalSphereItem.PlaceItem(this);
		_items.Add(crystalSphereItem);
		IEnumerable<PotionModel> potionOptions = PotionFactory.GetPotionOptions(_owner, ModelDb.AllPotions.Where((PotionModel p) => p.Rarity == PotionRarity.Common));
		for (int num = 0; num < 2; num++)
		{
			CrystalSphereItem crystalSphereItem2 = new CrystalSpherePotion(this, Rng.NextItem(potionOptions).ToMutable());
			flag = flag && crystalSphereItem2.PlaceItem(this);
			_items.Add(crystalSphereItem2);
		}
		IEnumerable<PotionModel> potionOptions2 = PotionFactory.GetPotionOptions(_owner, ModelDb.AllPotions.Where((PotionModel p) => p.Rarity == PotionRarity.Rare));
		CrystalSphereItem crystalSphereItem3 = new CrystalSpherePotion(this, Rng.NextItem(potionOptions2).ToMutable());
		flag = flag && crystalSphereItem3.PlaceItem(this);
		_items.Add(crystalSphereItem3);
		CrystalSphereItem crystalSphereItem4 = new CrystalSphereCardReward(this, CardRarity.Common, _owner);
		flag = flag && crystalSphereItem4.PlaceItem(this);
		_items.Add(crystalSphereItem4);
		CrystalSphereItem crystalSphereItem5 = new CrystalSphereCardReward(this, CardRarity.Uncommon, _owner);
		flag = flag && crystalSphereItem5.PlaceItem(this);
		_items.Add(crystalSphereItem5);
		CrystalSphereItem crystalSphereItem6 = new CrystalSphereCardReward(this, CardRarity.Rare, _owner);
		flag = flag && crystalSphereItem6.PlaceItem(this);
		_items.Add(crystalSphereItem6);
		CrystalSphereItem crystalSphereItem7 = new CrystalSphereCurse();
		flag = flag && crystalSphereItem7.PlaceItem(this);
		_items.Add(crystalSphereItem7);
		for (int num2 = 0; num2 < 5; num2++)
		{
			CrystalSphereItem crystalSphereItem8 = new CrystalSphereGold(this, isBig: false);
			flag = flag && crystalSphereItem8.PlaceItem(this);
			_items.Add(crystalSphereItem8);
		}
		for (int num3 = 0; num3 < 2; num3++)
		{
			CrystalSphereItem crystalSphereItem9 = new CrystalSphereGold(this, isBig: true);
			flag = flag && crystalSphereItem9.PlaceItem(this);
			_items.Add(crystalSphereItem9);
		}
		return flag;
	}

	public void SetHoveredCell(CrystalSphereCell cell)
	{
		if (HoveredCell != null)
		{
			UnsetHoveredCell();
		}
		HoveredCell = cell;
		HoveredCell.IsHovered = true;
		if (CrystalSphereTool == CrystalSphereToolType.Big)
		{
			HighlightedCells = (from c in GetAdjacentCells(cell.X, cell.Y)
				select cells[c.X, c.Y]).ToList();
		}
		else
		{
			int num = 1;
			List<CrystalSphereCell> list = new List<CrystalSphereCell>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<CrystalSphereCell> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = cells[cell.X, cell.Y];
			HighlightedCells = list;
		}
		foreach (CrystalSphereCell highlightedCell in HighlightedCells)
		{
			highlightedCell.IsHighlighted = true;
		}
	}

	public void UnsetHoveredCell()
	{
		foreach (CrystalSphereCell highlightedCell in HighlightedCells)
		{
			highlightedCell.IsHighlighted = false;
			highlightedCell.IsHovered = false;
		}
		HighlightedCells = new List<CrystalSphereCell>();
		HoveredCell = null;
	}

	public void SetTool(CrystalSphereToolType tool)
	{
		CrystalSphereTool = tool;
		if (HoveredCell != null)
		{
			SetHoveredCell(HoveredCell);
		}
	}

	public async Task CellClicked(CrystalSphereCell clickedCell)
	{
		DivinationCount--;
		if (CrystalSphereTool != CrystalSphereToolType.Big)
		{
			await ClearCell(clickedCell.X, clickedCell.Y);
		}
		else
		{
			List<(int X, int Y)> adjacentCells = GetAdjacentCells(clickedCell.X, clickedCell.Y);
			foreach ((int X, int Y) item in adjacentCells)
			{
				await ClearCell(item.X, item.Y);
			}
		}
		if (DivinationCount == 0)
		{
			_completionSource.SetResult();
		}
	}

	private async Task ClearCell(int x, int y)
	{
		if (x < 0 || x >= GridSize.X)
		{
			throw new ArgumentException($"[{x},{y}] is not a valid cell on this grid");
		}
		if (y < 0 || y >= GridSize.Y)
		{
			throw new ArgumentException($"[{x},{y}] is not a valid cell on this grid");
		}
		if (!cells[x, y].IsHidden)
		{
			return;
		}
		cells[x, y].IsHidden = false;
		if (cells[x, y].Item != null)
		{
			CrystalSphereItem item = cells[x, y].Item;
			if (AreAllOccupiedCellsClear(item))
			{
				await item.RevealItem(_owner);
			}
		}
	}

	private bool AreAllOccupiedCellsClear(CrystalSphereItem item)
	{
		for (int i = 0; i < item.Size.X; i++)
		{
			for (int j = 0; j < item.Size.Y; j++)
			{
				int num = item.Position.X + i;
				int num2 = item.Position.Y + j;
				if (cells[num, num2].IsHidden)
				{
					return false;
				}
			}
		}
		return true;
	}

	public void AddReward(Reward reward)
	{
		_rewards.Add(reward);
	}

	private async Task CompleteMinigame()
	{
		await Cmd.Wait(0.75f);
		RewardsCmd.OfferCustom(_owner, _rewards);
		this.Finished?.Invoke();
	}

	private List<(int X, int Y)> GetAdjacentCells(int x, int y)
	{
		return GetHorizontalCells(x, y).Concat(GetVerticalCells(x, y)).Concat(GetDiagonalCells(x, y)).Concat(new global::_003C_003Ez__ReadOnlySingleElementList<(int X, int Y)>((x, y)))
			.ToList();
	}

	private List<(int X, int Y)> GetHorizontalCells(int x, int y)
	{
		List<(int X, int Y)> list = new List<(int X, int Y)>();
		for (int i = -1; i <= 1; i += 2)
		{
			int num = x + i;
			if (num >= 0 && num < 11)
			{
				list.Add((num, y));
			}
		}
		return list;
	}

	private List<(int X, int Y)> GetVerticalCells(int x, int y)
	{
		List<(int X, int Y)> list = new List<(int X, int Y)>();
		for (int i = -1; i <= 1; i += 2)
		{
			int num = y + i;
			if (num >= 0 && num < 11)
			{
				list.Add((x, num));
			}
		}
		return list;
	}

	private List<(int X, int Y)> GetDiagonalCells(int x, int y)
	{
		List<(int X, int Y)> list = new List<(int X, int Y)>();
		for (int i = -1; i <= 1; i += 2)
		{
			for (int j = -1; j <= 1; j += 2)
			{
				int num = x + i;
				int num2 = y + j;
				if (num >= 0 && num < 11 && num2 >= 0 && num2 < 11)
				{
					list.Add((num, num2));
				}
			}
		}
		return list;
	}
}
