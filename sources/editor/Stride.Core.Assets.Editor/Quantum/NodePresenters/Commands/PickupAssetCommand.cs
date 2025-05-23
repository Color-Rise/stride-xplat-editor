// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

public class PickupAssetCommand : ChangeValueWithPickerCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "PickupAsset";
    /// <summary>
    /// The current session.
    /// </summary>
    protected readonly SessionViewModel Session;

    /// <summary>
    /// Initializes a new instance of the <see cref="PickupAssetCommand"/> class.
    /// </summary>
    /// <param name="session">The current session.</param>
    public PickupAssetCommand(SessionViewModel session)
    {
        Session = session;
    }

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override CombineMode CombineMode => CombineMode.CombineOnlyForAll;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        var type = nodePresenter.Descriptor.GetInnerCollectionType();
        return AssetRegistry.CanBeAssignedToContentTypes(type, checkIsUrlType: true);
    }

    /// <inheritdoc/>
    protected override async Task<PickerResult> ShowPicker(IReadOnlyCollection<INodePresenter> nodePresenters, object currentValue, object parameter)
    {
        // FIXME xplat-editor
        //var targetType = (Type)parameter;
        //var assetPicker = Session.Dispatcher.Invoke(() => Session.ServiceProvider.Get<IEditorDialogService>().CreateAssetPickerDialog(Session));
        //assetPicker.Filter = x => FilterAsset(x, targetType);

        //var asset = GetCurrentTarget(currentValue);
        //if (asset != null)
        //{
        //    assetPicker.InitialLocation = asset.Directory;
        //    assetPicker.InitialAsset = asset;
        //}
        //else
        //{
        //    assetPicker.InitialLocation = Session.AssetCollection.SelectedAssets.Select(x => x.Directory).FirstOrDefault();
        //}
        //assetPicker.AllowMultiSelection = false;

        //var assetTypes = GetAssetTypes(targetType);
        //assetPicker.AcceptedTypes.AddRange(assetTypes);

        //var result = await assetPicker.ShowModal();

        var pickerResult = new PickerResult
        {
            // FIXME xplat-editor
            //ProcessChange = result == DialogResult.Ok,
            //NewValue = result == DialogResult.Ok ? assetPicker.SelectedAssets.FirstOrDefault() : null
        };
        return pickerResult;
    }

    /// <inheritdoc/>
    protected override object ConvertPickerValue(object newValue, object parameter)
    {
        var targetType = (Type)parameter;
        return CreateReference((AssetViewModel)newValue, targetType);
    }

    /// <summary>
    /// Filters assets to display in the picker.
    /// </summary>
    /// <param name="asset">The asset to filter.</param>
    /// <param name="referenceType">The type of reference this command is targeting.</param>
    /// <returns>True if this asset should be displayed, False otherwise.</returns>
    protected virtual bool FilterAsset(AssetViewModel asset, Type referenceType)
    {
        return true;
    }

    protected virtual IEnumerable<Type> GetAssetTypes(Type contentType)
    {
        if (AssetRegistry.CanPropertyHandleAssets(contentType, out var types))
            return types;
        return [];
    }

    protected virtual AssetViewModel GetCurrentTarget(object currentValue)
    {
        return ContentReferenceHelper.GetReferenceTarget(Session, currentValue);
    }

    protected virtual object CreateReference(AssetViewModel asset, Type referenceType)
    {
        return ContentReferenceHelper.CreateReference(asset, referenceType);
    }
}
