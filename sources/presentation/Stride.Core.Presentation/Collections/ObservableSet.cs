// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Collections;

public class ObservableSet<T> : ObservableCollection<T>, IObservableList<T>, IReadOnlyObservableList<T>
{
    private readonly HashSet<T> hashSet;

    [CollectionAccess(CollectionAccessType.None)]
    public ObservableSet()
        : this(EqualityComparer<T>.Default)
    {
    }

    [CollectionAccess(CollectionAccessType.UpdatedContent)]
    public ObservableSet(IEnumerable<T> collection)
        : this(EqualityComparer<T>.Default, collection)
    {
    }

    [CollectionAccess(CollectionAccessType.None)]
    public ObservableSet(IEqualityComparer<T> comparer)
    {
        hashSet = new HashSet<T>(comparer);
    }

    [CollectionAccess(CollectionAccessType.UpdatedContent)]
    public ObservableSet(IEqualityComparer<T> comparer, IEnumerable<T> collection)
    {
        hashSet = new HashSet<T>(comparer);
        AddRange(collection);
    }

    [CollectionAccess(CollectionAccessType.UpdatedContent)]
    public void AddRange(IEnumerable<T> items)
    {
        var itemList = items.Where(x => hashSet.Add(x)).ToList();
        if (itemList.Count > 0)
        {
            foreach (var item in itemList)
            {
                Items.Add(item);
            }

            OnCountPropertyChanged();
            OnIndexerPropertyChanged();
            var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemList, Count - itemList.Count);
            OnCollectionChanged(arg);
        }
    }

    protected override void ClearItems()
    {
        hashSet.Clear();
        base.ClearItems();
    }

    protected override void InsertItem(int index, T item)
    {
        if (hashSet.Add(item))
        {
            base.InsertItem(index, item);
        }
    }

    protected override void SetItem(int index, T item)
    {
        var oldItem = base[index];
        hashSet.Remove(oldItem);
        if (!hashSet.Add(item))
        {
            // restore removed item
            hashSet.Add(oldItem);
            throw new InvalidOperationException("Unable to set this value at the given index because this value is already contained in this ObservableSet.");
        }
        base.SetItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        var item = base[index];
        hashSet.Remove(item);
        base.RemoveItem(index);
    }

    /// <inheritdoc/>
    [CollectionAccess(CollectionAccessType.None)]
    public override string ToString()
    {
        return $"{{ObservableSet}} Count = {Count}";
    }

    /// <summary>
    /// Helper to raise a PropertyChanged event for the Count property
    /// </summary>
    private void OnCountPropertyChanged() => OnPropertyChanged(new PropertyChangedEventArgs("Count"));

    /// <summary>
    /// Helper to raise a PropertyChanged event for the Indexer property
    /// </summary>
    private void OnIndexerPropertyChanged() => OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
}
