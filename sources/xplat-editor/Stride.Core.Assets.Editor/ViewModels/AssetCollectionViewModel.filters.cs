// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public enum DisplayAssetMode
{
    AssetInSelectedFolderOnly,
    AssetInSelectedFolderAndSubFolder,
    AssetAndFolderInSelectedFolder,
}

public enum FilterCategory
{
    AssetName,
    AssetTag,
    AssetType,
}

public enum SortRule
{
    Name,
    TypeOrderThenName,
    DirtyThenName,
    ModificationDateThenName,
}

partial class AssetCollectionViewModel
{
    private readonly ObservableSet<AssetFilterViewModel> currentAssetFilters = new();
    private Func<AssetViewModel, bool> customFilter;

    // /!\ FIXME: we need to rework that as we probably don't need that many lists
    private readonly ObservableList<AssetViewModel> filteredAssets = new();
    private readonly ObservableList<object> filteredContent = new();

    private bool refreshing;

    private DisplayAssetMode displayMode = DisplayAssetMode.AssetAndFolderInSelectedFolder;
    private SortRule sortRule = SortRule.TypeOrderThenName;
    // FIXME xplat-editor
    //private DisplayAssetMode displayMode = InternalSettings.AssetViewDisplayMode.GetValue();
    //private SortRule sortRule = InternalSettings.AssetViewSortRule.GetValue();

    public DisplayAssetMode DisplayAssetMode { get => displayMode; private set => SetValue(ref displayMode, value, UpdateLocations); }

    public IReadOnlyObservableCollection<object> FilteredContent => filteredContent;

    public SortRule SortRule { get => sortRule; private set => SetValue(ref sortRule, value, RefreshFilters); }

    private static string[] ComputeTokens(string pattern)
    {
        return pattern?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
    }

    private void RefreshFilters()
    {
        if (!refreshing)
        {
            refreshing = true;
            filteredAssets.Clear();
            // Add assets either that matches the filter or are currently selected.
            var filteredList = Assets.Where(x => selectedAssets.Contains(x) || Match(x)).ToList();
            var nameComparer = new NaturalStringComparer();
            var assetNameComparer = new AnonymousComparer<AssetViewModel>((x, y) => nameComparer.Compare(x?.Name, y?.Name));
            var comparer = SortRule switch
            {
                SortRule.TypeOrderThenName => new AnonymousComparer<AssetViewModel>((x, y) =>
                {
                    var r = -(DisplayAttribute.GetOrder(x.AssetType) ?? 0).CompareTo(DisplayAttribute.GetOrder(y.AssetType) ?? 0);
                    return r == 0 ? assetNameComparer.Compare(x, y) : r;
                }),
                SortRule.DirtyThenName => new AnonymousComparer<AssetViewModel>((x, y) =>
                {
                    var r = -x?.IsDirty.CompareTo(y?.IsDirty);
                    return (r ?? 0) == 0 ? assetNameComparer.Compare(x, y) : (r ?? 0);
                }),
                SortRule.ModificationDateThenName => new AnonymousComparer<AssetViewModel>((x, y) =>
                {
                    var r = -x?.AssetItem.ModifiedTime.CompareTo(y?.AssetItem.ModifiedTime);
                    return (r ?? 0) == 0 ? assetNameComparer.Compare(x, y) : (r ?? 0);
                }),
                _ => assetNameComparer, // Sort by name by default.
            };
            filteredList.Sort(comparer);
            filteredAssets.AddRange(filteredList);
            refreshing = false;

            // Force updating the filtered content
            UpdateFilteredContent();
        }

        // FIXME xplat-editor
        //InternalSettings.AssetViewSortRule.SetValue(SortRule);

        bool Match(AssetViewModel asset)
        {
            if (customFilter?.Invoke(asset) ?? false)
                return false;

            // Type filters are OR-ed
            var activeTypeFilters = currentAssetFilters.Where(f => f.Category == FilterCategory.AssetType && f.IsActive).ToList();
            if (activeTypeFilters.Count > 0 && !activeTypeFilters.Any(f => f.Match(asset)))
                return false;

            // Name and tag filters are AND-ed
            if (!currentAssetFilters.Where(f => f.Category != FilterCategory.AssetType && f.IsActive).All(f => f.Match(asset)))
                return false;

            return true;
        }
    }

    private void RemoveAssetFilter(AssetFilterViewModel? filter)
    {
        if (filter is null)
            return;

        filter.IsActive = false;
        currentAssetFilters.Remove(filter);
    }

    private void UpdateFilteredContent()
    {
        // simple implementation: rebuild the FilteredContent collection each time (could be improve)
        filteredContent.Clear();

        if (DisplayAssetMode != DisplayAssetMode.AssetAndFolderInSelectedFolder)
        {
            filteredContent.AddRange(filteredAssets);
            return;
        }

        // Filter folders by name
        IEnumerable<object> folders = monitoredDirectories.SelectMany(d => d.SubDirectories)
            .Where(d => currentAssetFilters.Where(f => f.Category == FilterCategory.AssetName && f.IsActive)
                .All(f => ComputeTokens(f.Filter).All(x => d.Name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)));
        filteredContent.AddRange(folders.Concat(filteredAssets));
    }

    public sealed class AssetFilterViewModel : DispatcherViewModel, IEquatable<AssetFilterViewModel>
    {
        private readonly AssetCollectionViewModel collection;
        private bool isActive;
        private bool isReadOnly;

        public AssetFilterViewModel(AssetCollectionViewModel collection, FilterCategory category, string filter, string? displayName)
            : base(collection.ServiceProvider)
        {
            this.collection = collection;
            Category = category;
            DisplayName = displayName;
            Filter = filter;
            isActive = true;

            RemoveFilterCommand = new AnonymousCommand<AssetFilterViewModel>(ServiceProvider, collection.RemoveAssetFilter);
            ToggleIsActiveCommand = new AnonymousCommand(ServiceProvider, () => IsActive = !IsActive);
        }

        public FilterCategory Category { get; }

        public string? DisplayName { get; }

        public string Filter { get; }

        public bool IsActive
        {
            get => isActive;
            set => SetValue(ref isActive, value, collection.RefreshFilters);
        }

        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                SetValue(ref isReadOnly, value, () =>
                {
                    RemoveFilterCommand.IsEnabled = !value;
                    ToggleIsActiveCommand.IsEnabled = !value;
                });
            }
        }

        public ICommandBase RemoveFilterCommand { get; }

        public ICommandBase ToggleIsActiveCommand { get; }

        public bool Equals(AssetFilterViewModel? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Category == other.Category && string.Equals(Filter, other.Filter, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as AssetFilterViewModel);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Category * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Filter);
            }
        }

        public bool Match(AssetViewModel asset)
        {
            switch (Category)
            {
                case FilterCategory.AssetName:
                    return ComputeTokens(Filter).All(x => asset.Name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);

                case FilterCategory.AssetTag:
                    return asset.Tags.Any(y => y.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0);

                case FilterCategory.AssetType:
                    return string.Equals(asset.AssetType.FullName, Filter);
            }
            return false;
        }

        public static bool operator ==(AssetFilterViewModel left, AssetFilterViewModel right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AssetFilterViewModel left, AssetFilterViewModel right)
        {
            return !Equals(left, right);
        }
    }
}
