// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Presentation.Components.Properties;

/// <summary>
/// This class manages the construction of <see cref="GraphViewModel"/> of a selection of objects.
/// </summary>
public abstract class PropertiesViewModel : DispatcherViewModel
{
    protected readonly GraphViewModelService ViewModelService;
    protected readonly NodeContainer NodeContainer;
    private readonly object lockObject = new();

    private GraphViewModel viewModel;
    private bool canDisplayProperties;
    private string fallbackMessage;
    private int currentToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertiesViewModel"/> class.
    /// </summary>
    /// <param name="serviceProvider">The view model service provider to use.</param>
    /// <param name="nodeContainer">The <see cref="NodeContainer"/> containing the nodes used to access object properties.</param>
    protected PropertiesViewModel(IViewModelServiceProvider serviceProvider, NodeContainer nodeContainer)
        : base(serviceProvider)
    {
        NodeContainer = nodeContainer;

        // Create the service needed to manage graph view models
        ViewModelService = new GraphViewModelService(nodeContainer);

        // Update the service provider of this view model to contains the GraphViewModelService we created.
        ServiceProvider = new ViewModelServiceProvider(serviceProvider, ViewModelService.Yield());

		// FIXME xplat-editor uncomment following lines
        //RegisterNodePresenterCommand(new CreateNewInstanceCommand());
        //RegisterNodePresenterCommand(new AddNewItemCommand());
        //RegisterNodePresenterCommand(new AddPrimitiveKeyCommand());
        //RegisterNodePresenterCommand(new RemoveItemCommand());
        //RegisterNodePresenterCommand(new RenameStringKeyCommand());
        //RegisterNodePresenterCommand(new MoveItemCommand());
        //RegisterNodePresenterCommand(new FlagEnumSelectAllCommand());
        //RegisterNodePresenterCommand(new FlagEnumSelectNoneCommand());
        //RegisterNodePresenterCommand(new FlagEnumSelectInvertCommand());

        //RegisterNodePresenterUpdater(new AbstractNodeEntryNodeUpdater());
        //RegisterNodePresenterUpdater(new CategoryNodeUpdater());
        //RegisterNodePresenterUpdater(new CollectionPropertyNodeUpdater());
        //RegisterNodePresenterUpdater(new DisplayAttributeNodeUpdater());
        //RegisterNodePresenterUpdater(new MathematicsNodeUpdater());
        //RegisterNodePresenterUpdater(new NumericValueNodeUpdater());
        //RegisterNodePresenterUpdater(new UPathNodeUpdater());
        //RegisterNodePresenterUpdater(new DictionaryNodeUpdater());

        // Note: try to keep InlineMemberNodeUpdater last since it's transfering some metadata generated by previous updater between nodes
        //RegisterNodePresenterUpdater(new InlineMemberNodeUpdater());
    }

    /// <summary>
    /// Gets the current instance of <see cref="GraphViewModel"/> contained in the <see cref="PropertiesViewModel"/>.
    /// </summary>
    public GraphViewModel ViewModel { get { return viewModel; } private set { SetValue(ref viewModel, value); } }

    public bool CanDisplayProperties { get { return canDisplayProperties; } set { SetValue(ref canDisplayProperties, value); } }

    public string FallbackMessage { get { return fallbackMessage; } set { SetValue(ref fallbackMessage, value); } }

    protected abstract string EmptySelectionFallbackMessage { get; }

    public IReadOnlyCollection<IPropertyProviderViewModel> Selection { get; private set; }

    // TODO: provide a more solid API to avoid duplicates and name collision
    public void RegisterNodePresenterCommand(INodePresenterCommand command) => ViewModelService.AvailableCommands.Add(command);

    public void UnregisterNodePresenterCommand(INodePresenterCommand command) => ViewModelService.AvailableCommands.Remove(command);

    public void RegisterNodePresenterUpdater(INodePresenterUpdater nodeUpdater) => ViewModelService.AvailableUpdaters.Add(nodeUpdater);

    public void UnregisterNodePresenterUpdater(INodePresenterUpdater nodeUpdater) => ViewModelService.AvailableUpdaters.Remove(nodeUpdater);

    // TODO: remove processViewModel
    public async Task<GraphViewModel> GenerateSelectionPropertiesAsync(IEnumerable<IPropertyProviderViewModel> selectedObjects)
    {
        // This method must be called from the UI thread to avoid concurrency in the beginning part (before awaiting)
        Dispatcher.EnsureAccess();

        // This check must be done before any await
        //if (ServiceProvider.TryGet<SelectionService>()?.PropertySelectionSuppressed ?? false)
        //    return null;

        // Wait for current transactions or undo/redo to complete before continuing.
        var undoRedoService = ServiceProvider.TryGet<IUndoRedoService>();
        if (undoRedoService != null)
        {
            await Task.WhenAll(undoRedoService.TransactionCompletion, undoRedoService.UndoRedoCompletion);
        }

        ++currentToken;
        var localToken = currentToken;

        Selection = selectedObjects.ToList();

        CanDisplayProperties = false;

        ViewModel?.Destroy();

        // We set to null now, in case this async method is invoked again while not being finished (otherwise it would dispose twice!)
        ViewModel = null;

        // No selection, just clean up
        if (Selection.Count == 0)
        {
            ViewModel = null;
            FallbackMessage = EmptySelectionFallbackMessage;
            return null;
        }

        string message;
        if (!CanDisplaySelectedObjects(Selection, out message))
        {
            ViewModel = null;
            FallbackMessage = message;
            return null;
        }

        try
        {
            FallbackMessage = Tr._p("Properties", "Loading properties...");
            var newViewModel = await InitializeViewModel(Selection, localToken);
            if (localToken != currentToken)
                return null;

            ViewModel = newViewModel;
            CanDisplayProperties = true;
        }
        catch (Exception exception)
        {
            FeedbackException(Selection, exception, out message);
            FallbackMessage = message;
            ViewModel = null;
        }
        return ViewModel;
    }

    public Task RefreshSelectedPropertiesAsync()
    {
        return Selection != null ? GenerateSelectionPropertiesAsync(Selection) : Task.CompletedTask;
    }

    protected abstract bool CanDisplaySelectedObjects(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, out string fallbackMessage);

    protected abstract void FeedbackException(IReadOnlyCollection<IPropertyProviderViewModel> selectedObjects, Exception exception, out string fallbackMessage);

    private Task<GraphViewModel> InitializeViewModel(IReadOnlyCollection<IPropertyProviderViewModel> objects, int localToken)
    {
        return Task.Run(() =>
        {
            GraphViewModel newViewModel = null;
            if (currentToken == localToken)
            {
                lock (lockObject)
                {
                    newViewModel = GraphViewModel.Create(ServiceProvider, objects);
                }
            }
            return newViewModel;
        });
    }
}