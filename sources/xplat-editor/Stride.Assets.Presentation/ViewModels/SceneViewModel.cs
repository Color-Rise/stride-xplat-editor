// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

// NOTE should we allow deriving from this to have custom behavior in some 3rd-party plugins?
// if so, the class shouldn't be sealed

/// <summary>
/// View model for <see cref="SceneAsset"/>.
/// </summary>
[AssetViewModel<SceneAsset>]
public class SceneViewModel : EntityHierarchyViewModel, IAssetViewModel<SceneAsset>
{
    public SceneViewModel(AssetItem assetItem, DirectoryBaseViewModel directory)
        : base(assetItem, directory)
    {
    }

    /// <inheritdoc />
    public new SceneAsset Asset => (SceneAsset)base.Asset;
}
