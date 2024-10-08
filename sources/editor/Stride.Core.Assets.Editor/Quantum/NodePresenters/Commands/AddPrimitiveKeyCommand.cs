// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

public class AddPrimitiveKeyCommand : SyncNodePresenterCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "AddPrimitiveKey";

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override CombineMode CombineMode => CombineMode.CombineOnlyForAll;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        // We are in a dictionary...
        if (nodePresenter.Descriptor is not DictionaryDescriptor dictionaryDescriptor)
            return false;

        // ... that is not read-only...
        var memberCollection = (nodePresenter as MemberNodePresenter)?.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                               ?? nodePresenter.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
        if (memberCollection?.ReadOnly == true)
            return false;

        // ... can construct key type...
        if (!AddNewItemCommand.CanConstruct(dictionaryDescriptor.KeyType))
            return false;

        // ... and can construct value type
        var elementType = dictionaryDescriptor.ValueType;
        return AddNewItemCommand.CanAdd(elementType);
    }

    /// <inheritdoc/>
    protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
    {
        var assetNodePresenter = nodePresenter as IAssetNodePresenter;
        var dictionaryDescriptor = (DictionaryDescriptor)nodePresenter.Descriptor;
        var value = nodePresenter.Value;

        NodeIndex? newKey;
        if (parameter != null && TypeDescriptor.GetConverter(dictionaryDescriptor.KeyType).CanConvertFrom(parameter.GetType()))
            newKey = GenerateGenericKey(value, dictionaryDescriptor, parameter);
        else if (dictionaryDescriptor.KeyType.IsEnum)
            newKey = new NodeIndex(parameter);
        else
            newKey = new NodeIndex(Activator.CreateInstance(dictionaryDescriptor.KeyType));

        if (newKey != null)
        {
            if (!dictionaryDescriptor.ContainsKey(nodePresenter.Value, newKey.Value.Value))
            {
                var newItem = dictionaryDescriptor.ValueType.Default();
                var instance = CreateInstance(dictionaryDescriptor.ValueType);
                if (!AddNewItemCommand.IsReferenceType(dictionaryDescriptor.ValueType) && (assetNodePresenter == null || !assetNodePresenter.IsObjectReference(instance)))
                    newItem = instance;

                nodePresenter.AddItem(newItem, newKey.Value);
            }
        }
    }

    /// <summary>
    /// Creates an instance of the specified type using that type's default constructor.
    /// </summary>
    /// <param name="type">The type of object to create.</param>
    /// <returns>A reference to the newly created object.</returns>
    /// <seealso cref="Activator.CreateInstance(Type)"/>
    /// <exception cref="ArgumentNullException">type is null.</exception>
    private static object? CreateInstance(Type type)
    {
        // abstract type cannot be instantiated
        if (type.IsAbstract)
            return null;

        // string is a special case
        if (type == typeof(string))
            return string.Empty;

        // note:
        //      Type not having a public parameterless constructor will throw a MissingMethodException at this point.
        //      This is intended as YAML serialization requires this constructor.
        return ObjectFactoryRegistry.NewInstance(type);
    }

    internal static NodeIndex? GenerateGenericKey(object dictionary, ITypeDescriptor descriptor, object baseValue)
    {
        // TODO: use a dialog service and popup a message when the given key is invalid
        var dictionaryDescriptor = descriptor as DictionaryDescriptor;
        var keyType = dictionaryDescriptor.KeyType;
        var key = keyType.Default();

        key = TypeDescriptor.GetConverter(keyType).ConvertFrom(baseValue);

        if (!dictionaryDescriptor.ContainsKey(dictionary, key))
        {
            return new NodeIndex(key);
        }

        return null;
    }
}
