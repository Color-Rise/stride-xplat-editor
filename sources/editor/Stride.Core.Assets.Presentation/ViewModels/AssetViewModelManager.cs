// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Presentation.ViewModels;

// FIXME xplat-editor find a better name
// TODO this class is meant to centralize all asset view model manipumation (creation, deletion, movement between folders or packages, etc.)
public static class AssetViewModelManager
{
    // FIXME xplat-editor should this be moved to an utility in the editor project instead (asset project should have minimum capability)?
    //       We should otherwise a kind of default session view model that could be used in projects outside the editor with a minimum feature set
    public static AssetViewModel CreateAsset(AssetItem assetItem, DirectoryBaseViewModel directory, bool canUndoRedoCreation, ILogger? logger = null)
    {
        AssetCollectionItemIdHelper.GenerateMissingItemIds(assetItem.Asset);
        directory.Session.GraphContainer.InitializeAsset(assetItem, logger);
        var assetViewModelType = directory.Session.GetAssetViewModelType(assetItem);
        if (assetViewModelType.IsGenericType)
        {
            assetViewModelType = assetViewModelType.MakeGenericType(assetItem.Asset.GetType());
        }
        return (AssetViewModel)Activator.CreateInstance(assetViewModelType, new AssetViewModel.ConstructorParameters(assetItem, directory, canUndoRedoCreation))!;
    }

    /// <summary>
    /// Creates the view models for each asset, directory, profile, project and reference of <paramref name="package"/>.
    /// </summary>
    /// <param name="token">A cancellation token to cancel the load proces.</param>
    public static void LoadPackageInformation(PackageViewModel package, IProgressViewModel? progressVM, ref double progress, CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
            return;

        foreach (var asset in package.Package.Assets.ToList())
        {
            if (token.IsCancellationRequested)
                return;

            progressVM?.UpdateProgress($"Processing asset {progress + 1}/{progressVM.Maximum}...", progress);

            var url = asset.Location;
            DirectoryBaseViewModel directory;
            // TODO CSPROJ=XKPKG override rather than cast to subclass
            if (asset.Asset is IProjectAsset && package is ProjectViewModel project)
            {
                directory = project.GetOrCreateProjectDirectory(url.GetFullDirectory());
            }
            else
            {
                directory = package.GetOrCreateAssetDirectory(url.GetFullDirectory());
            }
            CreateAsset(asset, directory, false);
            progress++;
        }

        FillRootAssetCollection();

        foreach (var explicitDirectory in package.Package.ExplicitFolders)
        {
            package.GetOrCreateAssetDirectory(explicitDirectory);
        }

        return;

        void FillRootAssetCollection()
        {
            package.RootAssets.Clear();
            package.RootAssets.AddRange(package.Package.RootAssets.Select(x => package.Session.GetAssetById(x.Id)).NotNull());
            foreach (var dependency in package.PackageContainer.FlattenedDependencies)
            {
                package.RootAssets.AddRange(dependency.Package.RootAssets.Select(x => package.Session.GetAssetById(x.Id)).NotNull());
            }
        }
    }
}

/*
 * ideas:
 *
 * - [ ] make IsDeleted either virtual in SessionObjectViewModel,
 *   or add a virtual method SetIsDeleted() that is overriden in AssetViewModel to handle Add/RemoveAsset in directories/packages
 * - [x] move PackageViewModel.LoadPackageInformation() in this helper
 * - [ ] try to reduce the API surface of ISessionViewModel to make it simpler to provide alternative implementations
 *     - corollary: create a SimpleSessionViewModel with no undo/redo a simple node container etc. to be used as a readonly view of assets utility
 * 
 */
