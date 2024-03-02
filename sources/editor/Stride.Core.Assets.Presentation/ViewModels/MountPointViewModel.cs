// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class MountPointViewModel : DirectoryBaseViewModel
{
    protected MountPointViewModel(PackageViewModel package)
        : base(package.Session)
    {
        Package = package;
    }

    /// <inheritdoc/>
    public override IEnumerable<IDirtiable> Dirtiables => base.Dirtiables.Concat(Package.Dirtiables);

    public override PackageViewModel Package { get; }

    /// <inheritdoc/>
    public override DirectoryBaseViewModel Parent
    {
        get => null!;
        set => throw new InvalidOperationException($"Cannot change the parent of a {nameof(MountPointViewModel)}");
    }
    
    /// <inheritdoc/>
    public override string Path => string.Empty;
    
    /// <inheritdoc/>
    public override MountPointViewModel Root => this;

    /// <inheritdoc/>
    public override string TypeDisplayName => "Mount Point";

    public IReadOnlyCollection<DirectoryBaseViewModel> GetDirectoryHierarchy()
    {
        var hierarchy = new List<DirectoryBaseViewModel>();
        GetDirectoryHierarchy(hierarchy);
        return hierarchy;
    }
    
    /// <inheritdoc/>
    public override void Delete()
    {
        throw new NotSupportedException("Cannot delete a MountPointViewModel");
    }

    public abstract bool AcceptAssetType(Type assetType);
    
    /// <inheritdoc/>
    protected override void UpdateIsDeletedStatus()
    {
        throw new NotImplementedException();
    }
}
