// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.ViewModels;

/// <summary>
/// A view model class representing one or multiple trees of <see cref="INodePresenter"/> instances.
/// </summary>
public class GraphViewModel : DispatcherViewModel
{
    public const string DefaultLoggerName = "Quantum";
    public const string HasChildPrefix = "HasChild_";
    public const string HasCommandPrefix = "HasCommand_";
    public const string HasAssociatedDataPrefix = "HasAssociatedData_";
    private NodeViewModel rootNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphViewModel"/> class.
    /// </summary>
    /// <param name="serviceProvider">A service provider that can provide a <see cref="IDispatcherService"/> and an <see cref="GraphViewModelService"/> to use for this view model.</param>
    /// <param name="type"></param>
    /// <param name="rootPresenters">The root <see cref="INodePresenter"/> instances.</param>
    private GraphViewModel(IViewModelServiceProvider serviceProvider, Type type, IEnumerable<INodePresenter> rootPresenters)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(rootPresenters);
        GraphViewModelService = serviceProvider.TryGet<GraphViewModelService>()
            ?? throw new InvalidOperationException($"{nameof(GraphViewModel)} requires a {nameof(GraphViewModelService)} in the service provider.");
        Logger = GlobalLogger.GetLogger(DefaultLoggerName);
        var viewModelFactory = serviceProvider.Get<GraphViewModelService>().NodeViewModelFactory;
        viewModelFactory.CreateGraph(this, type, rootPresenters);
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        RootNode.Children.SelectDeep(x => x.Children).ForEach(x => x.Destroy());
        RootNode.Destroy();
    }

    public static GraphViewModel? Create(IViewModelServiceProvider serviceProvider, IReadOnlyCollection<IPropertyProviderViewModel> propertyProviders)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(propertyProviders);

        var rootNodes = new List<INodePresenter>();
        Type? type = null;
        var factory = serviceProvider.Get<GraphViewModelService>().NodePresenterFactory;
        foreach (var propertyProvider in propertyProviders)
        {
            if (!propertyProvider.CanProvidePropertiesViewModel)
                return null;

            var rootNode = propertyProvider.GetRootNode();
            if (rootNode == null)
                return null;

            if (type == null)
                type = rootNode.Type;
            else if (type != rootNode.Type)
                return null;

            var node = factory.CreateNodeHierarchy(rootNode, new GraphNodePath(rootNode), propertyProvider);
            rootNodes.Add(node);
        }

        if (propertyProviders.Count == 0 || type == null) throw new ArgumentException($"{nameof(propertyProviders)} cannot be empty.", nameof(propertyProviders));
        return new GraphViewModel(serviceProvider, type, rootNodes);
    }

    /// <summary>
    /// Gets the root node of this <see cref="GraphViewModel"/>.
    /// </summary>
    public NodeViewModel RootNode { get { return rootNode; } set { SetValue(ref rootNode, value); } }

    /// <summary>
    /// Gets the <see cref="GraphViewModelService"/> associated to this view model.
    /// </summary>
    public GraphViewModelService GraphViewModelService { get; }

    /// <summary>
    /// Gets the <see cref="Logger"/> associated to this view model.
    /// </summary>
    public Logger Logger { get; }

    /// <summary>
    /// Raised when the value of an <see cref="NodeViewModel"/> contained into this view model has changed.
    /// </summary>
    public event EventHandler<NodeViewModelValueChangedArgs>? NodeValueChanged;

    internal void NotifyNodeChanged(NodeViewModel node)
    {
        NodeValueChanged?.Invoke(this, new NodeViewModelValueChangedArgs(this, node));
    }
}
