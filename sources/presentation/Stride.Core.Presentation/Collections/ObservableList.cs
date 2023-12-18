// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Collections;

public class ObservableList<T> : ObservableCollection<T>, IObservableList<T>, IReadOnlyObservableList<T>
{
    [CollectionAccess(CollectionAccessType.None)]
    public ObservableList()
        : base()
    {
    }

    [CollectionAccess(CollectionAccessType.UpdatedContent)]
    public ObservableList(IEnumerable<T> collection)
        : base(collection)
    {
    }

    [CollectionAccess(CollectionAccessType.UpdatedContent)]
    public void AddRange(IEnumerable<T> items)
    {
        var itemList = items.ToList();
        if (Items is List<T> list)
        {
            list.AddRange(itemList);
        }
        else
        {
            foreach (var item in itemList)
            {
                Items.Add(item);
            }
        }

        if (itemList.Count > 0)
        {
            OnCountPropertyChanged();
            OnIndexerPropertyChanged();
            var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemList, Count - itemList.Count);
            OnCollectionChanged(arg);
        }
    }

    [CollectionAccess(CollectionAccessType.Read)]
    public int FindIndex(Predicate<T> match)
    {
        if (Items is List<T> list)
        {
            return list.FindIndex(match);
        }

        for (int i = 0; i < Count; i++)
        {
            if (match(Items[i])) return i;
        }
        return -1;
    }

    [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
    public void RemoveRange(int index, int count)
    {
        var oldItems = Items.Skip(index).Take(count).ToList();
        if (Items is List<T> list)
        {
            list.RemoveRange(index, count);
        }
        else
        {
            // slow algorithm, optimized from collection's end
            for (int i = 1; i <= count; ++i)
            {
                Items.RemoveAt(index + count - i);
            }
        }

        if (oldItems.Count > 0)
        {
            OnCountPropertyChanged();
            OnIndexerPropertyChanged();
            var arg = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index);
            OnCollectionChanged(arg);
        }
    }

    /// <inheritdoc/>
    [CollectionAccess(CollectionAccessType.None)]
    public override string ToString()
    {
        return $"{{ObservableList}} Count = {Count}";
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
