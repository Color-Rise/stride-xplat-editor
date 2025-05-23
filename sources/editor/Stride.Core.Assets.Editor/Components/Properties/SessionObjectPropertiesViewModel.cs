// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Presentation.Quantum.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Components.Properties;

/// <summary>
/// This class manages the construction of <see cref="Core.Presentation.Quantum.ViewModels.GraphViewModel"/> of a selection of objects that
/// belongs to a session. It adds specific associated data and command related to the session, and updates
/// the <see cref="SessionViewModel.ActiveProperties"/> property when it creates a new view model.
/// </summary>
public class SessionObjectPropertiesViewModel : PropertiesViewModel
{
    private static bool contextLock;
    private string typeDescription;
    private string? name;
    private bool showOverridesOnly;

    public SessionObjectPropertiesViewModel(SessionViewModel session)
        : base(session.ServiceProvider, session.AssetNodeContainer)
    {
        Session = session;

        // Register initialization handler that will fill the default associated data
        ViewModelService.NodePresenterFactory = new AssetNodePresenterFactory(NodeContainer.NodeBuilder, ViewModelService.AvailableCommands, ViewModelService.AvailableUpdaters);
        ViewModelService.NodeViewModelFactory = new AssetNodeViewModelFactory();

        var dialogService = ServiceProvider.Get<IDialogService>();
        // FIXME xplat-editor
        //var documentationService = session.ServiceProvider.Get<UserDocumentationService>();

        RegisterNodePresenterCommand(new CopyPropertyCommand());
        RegisterNodePresenterCommand(new PastePropertyCommand());
        RegisterNodePresenterCommand(new ReplacePropertyCommand());
        RegisterNodePresenterCommand(new BrowseDirectoryCommand(dialogService, new SessionInitialDirectoryProvider(session)));
        RegisterNodePresenterCommand(new BrowseFileCommand(dialogService, new SessionInitialDirectoryProvider(session)));
        RegisterNodePresenterCommand(new FetchAssetCommand(session));
        RegisterNodePresenterCommand(new PickupAssetCommand(session));
        RegisterNodePresenterCommand(new SetContentReferenceCommand());
        RegisterNodePresenterCommand(new ResetOverrideCommand());

        RegisterNodePresenterUpdater(new ArchetypeNodeUpdater());
        // FIXME xplat-editor
        //RegisterNodePresenterUpdater(new DocumentationNodeUpdater(documentationService));
        RegisterNodePresenterUpdater(new OwnerAssetUpdater());
        RegisterNodePresenterUpdater(new SessionNodeUpdater(session));
    }

    /// <summary>
    /// Gets the session associated to this instance.
    /// </summary>
    public SessionViewModel Session { get; }

    /// <summary>
    /// Gets or sets a string describing the type of the selected objects.
    /// </summary>
    public string TypeDescription { get => typeDescription; set => SetValue(ref typeDescription, value); }

    /// <summary>
    /// Gets or sets the name of the selected objects.
    /// </summary>
    public string Name { get => name ?? string.Empty; set => SetValue(ref name, value); }

    /// <summary>
    /// Gets or sets whether to display only overridden properties when the displayed asset has a base.
    /// </summary>
    public bool ShowOverridesOnly { get => showOverridesOnly; set => SetValue(ref showOverridesOnly, value); }

    protected sealed override string EmptySelectionFallbackMessage => "Select an object to display its properties.";

    /// <summary>
    /// Gets an enumeration of assets related to this <see cref="PropertiesViewModel"/>.
    /// </summary>
    /// <remarks>This method returns an empty enumeration by default. Derived class should override when relevant.</remarks>
    /// <returns>An enumeration of the related assets.</returns>
    public IEnumerable<AssetViewModel> GetRelatedAssets()
    {
        return Selection.OfType<IAssetPropertyProviderViewModel>().Select(x => x.RelatedAsset);
    }

    public void UpdateTypeAndName<T>(IEnumerable<T> selection, int count, Func<T, string> getObjType, Func<T, string> getObjName, string objTypePlural)
    {
        switch (count)
        {
            case 0:
                TypeDescription = "No selection";
                Name = "";
                break;

            case 1:
                var obj = selection.First();
                TypeDescription = getObjType(obj);
                Name = getObjName(obj);
                break;

            default:
                var types = selection.Select(getObjType).Distinct().ToList();
                TypeDescription = types.Count == 1 ? types.First() : "Multi-selection";
                Name = $"{count} {objTypePlural} selected";
                break;
        }
    }

    public void UpdateTypeAndName<T>(IReadOnlyCollection<T> selection, Func<T, string> getObjType, Func<T, string> getObjName, string objTypePlural)
    {
        UpdateTypeAndName(selection, selection.Count, getObjType, getObjName, objTypePlural);
    }

    protected sealed override bool CanDisplaySelectedObjects(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, out string? fallbackMessage)
    {
        foreach (var provider in selectedObjects.OfType<IAssetPropertyProviderViewModel>())
        {
            var asset = provider.RelatedAsset;

            // TODO: Support read-only mode
            if (!asset.IsEditable)
            {
                fallbackMessage = selectedObjects.Count == 1 ? "This asset is not editable." : "Some of the selected assets are not editable.";
                return false;
            }
        }
        fallbackMessage = null;
        return true;
    }

    protected sealed override void FeedbackException(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, Exception exception, out string? fallbackMessage)
    {
        fallbackMessage = Tr._p("Properties", "There was a problem loading properties of the selection.");
    }

    protected override void OnPropertyChanged(params string[] propertyNames)
    {
        base.OnPropertyChanged(propertyNames);
        // Do this only when the property has changed from the UI thread
        if (!Dispatcher.CheckAccess())
            return;

        if (!contextLock && propertyNames.Any(x => x is nameof(ViewModel) or nameof(CanDisplayProperties) or nameof(FallbackMessage)))
        {
            contextLock = true;
            if (Session.ActiveProperties != this)
            {
                // FIXME xplat-editor
                //Session.ActiveProperties.ClearSelection();
            }
            Session.ActiveProperties = this;
            contextLock = false;
        }
    }
}
