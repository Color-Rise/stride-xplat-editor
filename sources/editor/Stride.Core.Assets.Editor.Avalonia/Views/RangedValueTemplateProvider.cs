// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class RangedValueTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "RangedValueTemplateProvider";

    public override bool MatchNode(NodeViewModel node)
    {
        // We need at least a minimum and a maximum to display a slider, but we also rely on having explicit small and large steps to make sure that the
        // slider won't be between the whole integer range for instance.
        return node.Type.IsNumeric() && node.AssociatedData.ContainsKey(NumericData.Minimum) && node.AssociatedData.ContainsKey(NumericData.Maximum)
               && node.AssociatedData.ContainsKey(NumericData.SmallStep) && node.AssociatedData.ContainsKey(NumericData.LargeStep);
    }
}
