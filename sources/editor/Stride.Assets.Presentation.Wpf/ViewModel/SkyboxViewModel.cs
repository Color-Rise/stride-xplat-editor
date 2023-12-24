// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Skyboxes;

namespace Stride.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(SkyboxAsset))]
    public class SkyboxViewModel : AssetViewModel<SkyboxAsset>
    {
        public SkyboxViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }
    }
}
