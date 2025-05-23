// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters;

public class RootNodePresenter : NodePresenterBase
{
    protected readonly IObjectNode RootNode;

    public RootNodePresenter(INodePresenterFactoryInternal factory, IPropertyProviderViewModel? propertyProvider, IObjectNode rootNode)
        : base(factory, propertyProvider, null)
    {
        ArgumentNullException.ThrowIfNull(factory);
        RootNode = rootNode ?? throw new ArgumentNullException(nameof(rootNode));
        Name = "Root";
        DisplayName = string.Empty;

        foreach (var command in factory.AvailableCommands)
        {
            if (command.CanAttach(this))
                Commands.Add(command);
        }

        rootNode.ItemChanging += OnItemChanging;
        rootNode.ItemChanged += OnItemChanged;
        AttachCommands();
    }

    public override Type Type => RootNode.Type;

    public override NodeIndex Index => NodeIndex.Empty;

    public override bool IsEnumerable => RootNode.IsEnumerable;

    public override ITypeDescriptor Descriptor => RootNode.Descriptor;

    public override object Value => RootNode.Retrieve();

    protected override IObjectNode ParentingNode => RootNode;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            RootNode.ItemChanging -= OnItemChanging;
            RootNode.ItemChanged -= OnItemChanged;
        }

        base.Dispose(disposing);
    }

    public override void UpdateValue(object? newValue)
    {
        throw new NodePresenterException($"A {nameof(RootNodePresenter)} cannot have its own value updated.");
    }

    public override void AddItem(object value)
    {
        if (!RootNode.IsEnumerable)
            throw new NodePresenterException($"{nameof(RootNodePresenter)}.{nameof(AddItem)} cannot be invoked on objects that are not collection.");

        try
        {
            RootNode.Add(value);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
        }
    }

    public override void AddItem(object value, NodeIndex index)
    {
        if (!RootNode.IsEnumerable)
            throw new NodePresenterException($"{nameof(RootNodePresenter)}.{nameof(AddItem)} cannot be invoked on objects that are not collection.");

        try
        {
            RootNode.Add(value, index);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
        }
    }

    public override void RemoveItem(object value, NodeIndex index)
    {
        if (!RootNode.IsEnumerable)
            throw new NodePresenterException($"{nameof(RootNodePresenter)}.{nameof(RemoveItem)} cannot be invoked on objects that are not collection.");

        try
        {
            RootNode.Remove(value, index);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while removing an item to the node, see the inner exception for more information.", e);
        }
    }

    public override NodeAccessor GetNodeAccessor()
    {
        return new NodeAccessor(RootNode, NodeIndex.Empty);
    }

    private void OnItemChanging(object? sender, ItemChangeEventArgs e)
    {
        RaiseValueChanging(Value);
    }

    private void OnItemChanged(object? sender, ItemChangeEventArgs e)
    {
        Refresh();
        RaiseValueChanged(Value);
    }
}
