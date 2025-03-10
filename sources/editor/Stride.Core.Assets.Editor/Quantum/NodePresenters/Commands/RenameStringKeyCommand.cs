// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

public class RenameStringKeyCommand : SyncNodePresenterCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "RenameStringKey";

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override CombineMode CombineMode => CombineMode.AlwaysCombine;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        // We are in a dictionary...
        var collectionNode = (nodePresenter as ItemNodePresenter)?.OwnerCollection;
        if (collectionNode?.Descriptor is not DictionaryDescriptor dictionaryDescriptor)
            return false;

        // ... that is not read-only...
        var memberCollection = (collectionNode as MemberNodePresenter)?.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                               ?? collectionNode.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
        if (memberCollection?.ReadOnly == true)
            return false;

        // ... and is indexed by strings, or can be converted from string...
        if (dictionaryDescriptor.KeyType != typeof(string)
            && !TypeDescriptor.GetConverter(dictionaryDescriptor.KeyType).CanConvertFrom(typeof(string)))
        {
            return false;
        }

        // ... and supports remove and insert
        // TODO: ... and can remove items - we don't have this information yet in DictionaryDescriptor
        return true;
    }

    /// <inheritdoc/>
    protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
    {
        var currentValue = nodePresenter.Value;
        var collectionNode = ((ItemNodePresenter)nodePresenter).OwnerCollection;

        var DictionaryDescriptor = collectionNode.Descriptor as DictionaryDescriptor;
        var keyType = DictionaryDescriptor.KeyType;
        NodeIndex? newName = null;
        if (TypeDescriptor.GetConverter(keyType).CanConvertFrom(typeof(string)))
        {
            newName = AddPrimitiveKeyCommand.GenerateGenericKey(collectionNode.Value, collectionNode.Descriptor, parameter);
        }

        if (newName != null)
        {
            collectionNode.RemoveItem(nodePresenter.Value, nodePresenter.Index);

            collectionNode.AddItem(currentValue, newName.Value);
        }
    }
}
