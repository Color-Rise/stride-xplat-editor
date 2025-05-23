// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class SkeletonAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Asset is not SkeletonViewModel)
            return;

        if (typeof(NodeInformation).IsAssignableFrom(node.Type) && node.IsVisible)
        {
            // Hide all children
            foreach (var child in node.Children)
            {
                child.IsVisible = false;
            }
            var name = (string)node[nameof(NodeInformation.Name)].Value;
            var depth = (int)node[nameof(NodeInformation.Depth)].Value;
            // Set the display name to be the name of the node, indented using space.
            node.DisplayName = $"{"".PadLeft(2 * depth)}{name}";
            // Set the order to be the index, we don't want to sort alphabetically
            var index = (node as AssetItemNodePresenter)?.Index ?? NodeIndex.Empty;
            if (index.IsInt)
            {
                node.Order = index.Int;
            }
        }
    }
}
