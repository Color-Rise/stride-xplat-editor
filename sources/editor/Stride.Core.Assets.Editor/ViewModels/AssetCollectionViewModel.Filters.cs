// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
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
    public static readonly FilterCategory[] AllFilterCategories = Enum.GetValues<FilterCategory>();

    // /!\ FIXME: we need to rework that as we probably don't need that many lists
    private readonly ObservableList<AssetViewModel> filteredAssets = [];
    private readonly ObservableList<object> filteredContent = [];
    
    private DisplayAssetMode displayMode = InternalSettings.AssetViewDisplayMode.GetValue();
    private SortRule sortRule = InternalSettings.AssetViewSortRule.GetValue();

    private void RefreshFilters()
    {

    }

    public sealed class AssetFilterViewModel : DispatcherViewModel, IEquatable<AssetFilterViewModel>
    {
        private readonly AssetCollectionViewModel collection;
        private bool isActive;
        private bool isReadOnly;

        public AssetFilterViewModel(AssetCollectionViewModel collection, FilterCategory category, string filter, string? displayName)
            : base(collection.SafeArgument().ServiceProvider)
        {
            this.collection = collection;
            Category = category;
            DisplayName = displayName;
            Filter = filter;
            isActive = true;

            //RemoveFilterCommand = new AnonymousCommand<AssetFilterViewModel>(ServiceProvider, collection.RemoveAssetFilter);
            //ToggleIsActiveCommand = new AnonymousCommand(ServiceProvider, () => { IsActive = !IsActive; collection.SaveAssetFilters(); });
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
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Category == other.Category && string.Equals(Filter, other.Filter, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is AssetFilterViewModel other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Category, Filter);
        }

        public bool Match(AssetViewModel asset)
        {
            return Category switch
            {
                FilterCategory.AssetName => ComputeTokens(Filter).All(x => asset.Name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0),
                FilterCategory.AssetTag => asset.Tags.Any(y => y.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0),
                FilterCategory.AssetType => string.Equals(asset.AssetType.FullName, Filter),
                _ => false
            };
        }

        private static string[] ComputeTokens(string pattern)
        {
            return pattern?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        }
    }
}
