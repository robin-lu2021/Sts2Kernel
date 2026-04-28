using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CompilerGenerated]
public sealed class _003C_003Ez__ReadOnlyArray<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	private readonly T[] _items;

	int ICollection.Count => _items.Length;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get => _items[index];
		set => throw new NotSupportedException();
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => _items.Length;

	T IReadOnlyList<T>.this[int index] => _items[index];

	int ICollection<T>.Count => _items.Length;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get => _items[index];
		set => throw new NotSupportedException();
	}

	public _003C_003Ez__ReadOnlyArray(T[] items)
	{
		_items = items ?? Array.Empty<T>();
	}

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();

	void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

	int IList.Add(object? value) => throw new NotSupportedException();

	void IList.Clear() => throw new NotSupportedException();

	bool IList.Contains(object? value) => ((IList)_items).Contains(value);

	int IList.IndexOf(object? value) => ((IList)_items).IndexOf(value);

	void IList.Insert(int index, object? value) => throw new NotSupportedException();

	void IList.Remove(object? value) => throw new NotSupportedException();

	void IList.RemoveAt(int index) => throw new NotSupportedException();

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

	void ICollection<T>.Add(T item) => throw new NotSupportedException();

	void ICollection<T>.Clear() => throw new NotSupportedException();

	bool ICollection<T>.Contains(T item) => ((ICollection<T>)_items).Contains(item);

	void ICollection<T>.CopyTo(T[] array, int arrayIndex) => ((ICollection<T>)_items).CopyTo(array, arrayIndex);

	bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

	int IList<T>.IndexOf(T item) => ((IList<T>)_items).IndexOf(item);

	void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

	void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
}

[CompilerGenerated]
public sealed class _003C_003Ez__ReadOnlySingleElementList<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	private sealed class Enumerator : IEnumerator<T>
	{
		private readonly T _item;

		private bool _moveNextCalled;

		object IEnumerator.Current => _item!;

		public T Current => _item;

		public Enumerator(T item)
		{
			_item = item;
		}

		public bool MoveNext()
		{
			if (_moveNextCalled)
			{
				return false;
			}
			_moveNextCalled = true;
			return true;
		}

		public void Reset()
		{
			_moveNextCalled = false;
		}

		public void Dispose()
		{
		}
	}

	private readonly T _item;

	int ICollection.Count => 1;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set => throw new NotSupportedException();
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => 1;

	T IReadOnlyList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
	}

	int ICollection<T>.Count => 1;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set => throw new NotSupportedException();
	}

	public _003C_003Ez__ReadOnlySingleElementList(T item)
	{
		_item = item;
	}

	public _003C_003Ez__ReadOnlySingleElementList(T item, object? ignoredMetadata)
	{
		_item = item;
	}

	IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_item);

	void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);

	int IList.Add(object? value) => throw new NotSupportedException();

	void IList.Clear() => throw new NotSupportedException();

	bool IList.Contains(object? value) => value is T typed && EqualityComparer<T>.Default.Equals(_item, typed);

	int IList.IndexOf(object? value) => value is T typed && EqualityComparer<T>.Default.Equals(_item, typed) ? 0 : -1;

	void IList.Insert(int index, object? value) => throw new NotSupportedException();

	void IList.Remove(object? value) => throw new NotSupportedException();

	void IList.RemoveAt(int index) => throw new NotSupportedException();

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(_item);

	void ICollection<T>.Add(T item) => throw new NotSupportedException();

	void ICollection<T>.Clear() => throw new NotSupportedException();

	bool ICollection<T>.Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);

	void ICollection<T>.CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;

	bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

	int IList<T>.IndexOf(T item) => EqualityComparer<T>.Default.Equals(_item, item) ? 0 : -1;

	void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

	void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
}

[CompilerGenerated]
public sealed class _003C_003Ez__ReadOnlyList<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	private readonly List<T> _items;

	int ICollection.Count => _items.Count;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get => _items[index];
		set => throw new NotSupportedException();
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => _items.Count;

	T IReadOnlyList<T>.this[int index] => _items[index];

	int ICollection<T>.Count => _items.Count;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get => _items[index];
		set => throw new NotSupportedException();
	}

	public _003C_003Ez__ReadOnlyList(List<T> items)
	{
		_items = items ?? new List<T>();
	}

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();

	void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

	int IList.Add(object? value) => throw new NotSupportedException();

	void IList.Clear() => throw new NotSupportedException();

	bool IList.Contains(object? value) => ((IList)_items).Contains(value);

	int IList.IndexOf(object? value) => ((IList)_items).IndexOf(value);

	void IList.Insert(int index, object? value) => throw new NotSupportedException();

	void IList.Remove(object? value) => throw new NotSupportedException();

	void IList.RemoveAt(int index) => throw new NotSupportedException();

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

	void ICollection<T>.Add(T item) => throw new NotSupportedException();

	void ICollection<T>.Clear() => throw new NotSupportedException();

	bool ICollection<T>.Contains(T item) => _items.Contains(item);

	void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

	bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

	int IList<T>.IndexOf(T item) => _items.IndexOf(item);

	void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

	void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
}
