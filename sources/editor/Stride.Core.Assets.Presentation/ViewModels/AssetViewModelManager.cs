// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Presentation.ViewModels;

// FIXME xplat-editor find a better name
// TODO this class is meant to centralize all asset view model manipumation (creation, deletion, movement between folders or packages, etc.)
public sealed class AssetViewModelManager
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
}
