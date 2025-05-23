// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// An implementation of the <see cref="NodeViewModelTemplateProvider"/> that matches <see cref="NodeViewModel"/> of a specific type.
/// </summary>
public class TypeMatchTemplateProvider : NodeViewModelTemplateProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeMatchTemplateProvider"/> class.
    /// </summary>
    public TypeMatchTemplateProvider()
    {
        AcceptNullable = true;
    }

    /// <summary>
    /// Gets or sets the <see cref="Type"/> to match. This provider will accept any node that has either a <see cref="NodeViewModel.Type"/>
    /// or a <see cref="NodeViewModel.NodeValue"/> with a type that is assignable to the type represented in this property.
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// Gets or sets whether to match nullable instances of the <see cref="Type"/>, when it represents a value type.
    /// </summary>
    public bool AcceptNullable { get; set; }

    /// <inheritdoc/>
    public override string Name => Type?.Name ?? "Undefined";

    /// <inheritdoc/>
    public override bool MatchNode(NodeViewModel node)
    {
        if (Type is null)
            return true;

        if (MatchType(node, Type))
            return true;

        if (AcceptNullable && Type.IsValueType)
        {
            var nullableType = typeof(Nullable<>).MakeGenericType(Type);
            return MatchType(node, nullableType);
        }

        return false;
    }
}
