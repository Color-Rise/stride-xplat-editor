// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels;

internal sealed class EntityReferenceViewModel : AddReferenceViewModel
{
    public override bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        EntityViewModel? entity = null;
        bool singleChild = true;
        foreach (var child in children)
        {
            if (!singleChild)
            {
                message = "Multiple entities selected";
                return false;
            }
            entity = child as EntityViewModel;
            if (entity == null)
            {
                message = "The selection is not an entity";
                return false;
            }

            singleChild = false;
        }
        if (entity == null)
        {
            message = "The selection is not an entity";
            return false;
        }
        message = $"Reference {entity.Name}";
        return true;
    }

    public override void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        var subEntity = (EntityViewModel)children.First();
        var command = TargetNode.GetCommand(SetEntityReferenceCommand.CommandName);
        command?.Execute(subEntity);
    }
}
