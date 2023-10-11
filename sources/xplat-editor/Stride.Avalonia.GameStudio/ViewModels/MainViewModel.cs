using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Stride.Avalonia.GameStudio.Views;

namespace Stride.Avalonia.GameStudio.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private IRootDock layout;
    private readonly DockFactory factory;
    private string? message;

    public MainViewModel()
    {
        factory = new DockFactory();
        layout = factory.CreateLayout();
        factory.InitLayout(layout);

        AboutCommand = new AsyncRelayCommand(OnAbout, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        ExitCommand = new RelayCommand(OnExit, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        OpenCommand = new RelayCommand(OnOpen);

        //AddBottomToolTab(SolutionExplorer = new SolutionExplorerViewModel());
    }

    public SolutionExplorerViewModel SolutionExplorer { get; }

    public IRootDock Layout
    {
        get => layout;
        set => SetProperty(ref layout, value);
    }

    public string? Message
    {
        get => message;
        set => SetProperty(ref message, value);
    }

    public ICommand AboutCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand OpenCommand { get; }

    private async Task OnAbout()
    {
        // FIXME: hide implementation details through a dialog service
        var window = new AboutWindow();
        await window.ShowDialog(((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!);
    }

    private void OnExit()
    {
        ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).TryShutdown();
    }

    private void OnOpen()
    {
        Message = "Clicked on Open";
    }

    #region Docks

    private void AddBottomToolTab(ViewModelBase context)
    {
        var bottom = factory.GetDockable<IProportionalDock>("Bottom");
        if (layout is {} && bottom is {})
        {
            var tool = new ToolTabViewModel
            {
                Context = context,
            };
            factory.AddDockable(bottom, tool);
            factory.SetActiveDockable(tool);
            factory.SetFocusedDockable(Layout, tool);
        }
    }

    private class DockFactory : Factory
    {
        private IRootDock? rootDock;

        private IProportionalDock? mainLayout;
        private IProportionalDock? topDock;
        private IProportionalDock? bottomDock;

        private IDocumentDock? documentDock;

        private IToolDock? assetExplorer;
        private IToolDock? assetPreview;
        private IToolDock? projectExplorer;
        private IToolDock? propertyGrid;

        public override IRootDock CreateLayout()
        {
            topDock = new ProportionalDock
            {
                Id = "Top",
                Orientation = Orientation.Horizontal,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    documentDock = new DocumentDock
                    {
                        Id = "Editors",
                        VisibleDockables = CreateList<IDockable>
                        (
                            new EditorTabViewModel()
                        )
                    },
                    new ProportionalDockSplitter(),
                    propertyGrid = new ToolDock
                    {
                        Id = "PropertyGrid",
                        VisibleDockables = CreateList<IDockable>
                        (
                            new ToolTabViewModel()
                        )
                    }
                )
            };
            bottomDock = new ProportionalDock
            {
                Id = "Bottom",
                Orientation = Orientation.Horizontal,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    projectExplorer = new ToolDock
                    {
                        Id = "ProjectExplorer",
                        VisibleDockables = CreateList<IDockable>
                        (
                            new ToolTabViewModel()
                        )
                    },
                    new ProportionalDockSplitter(),
                    assetExplorer = new ToolDock
                    {
                        Id = "AssetExplorer",
                        VisibleDockables = CreateList<IDockable>
                        (
                            new ToolTabViewModel()
                        )
                    },
                    new ProportionalDockSplitter(),
                    assetPreview = new ToolDock
                    {
                        Id = "AssetPreview",
                        VisibleDockables = CreateList<IDockable>
                        (
                            new ToolTabViewModel()
                        )
                    }
                )
            };
            mainLayout = new ProportionalDock
            {
                Id = "Main",
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>(
                    topDock,
                    new ProportionalDockSplitter(),
                    bottomDock
                )
            };

            rootDock = CreateRootDock();
            rootDock.Id = "Root";
            rootDock.IsCollapsable = false;
            rootDock.ActiveDockable = mainLayout;
            rootDock.DefaultDockable = mainLayout;

            return rootDock;
        }

        public override void InitLayout(IDockable layout)
        {
            DockableLocator = new Dictionary<string, Func<IDockable?>>
            {
                ["Root"] = () => rootDock,
                ["Main"] = () => mainLayout,
                ["Top"] = () => topDock,
                ["Bottom"] = () => bottomDock,
                ["Editors"] = () => documentDock,
                ["AssetExplorer"] = () => assetExplorer,
                ["AssetPreview"] = () => assetPreview,
                ["ProjectExplorer"] = () => projectExplorer,
                ["PropertyGrid"] = () => propertyGrid,
            };

            HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
            {
                [nameof(IDockWindow)] = () => new HostWindow()
            };

            base.InitLayout(layout);
        }
    }

    // 2023-10-09
    // Wed will have some known containers such as
    //  - Editors
    //      - acts as a container for all the editor which will be "documents"
    //  - PropertyGrid
    //  - SolutionExplorer
    //  - AssetExplorer
    //  - AssetPreview

    // 2023-10-09
    // below probably not needed
    // 2023-10-08
    // those are like controls, not view models
    // basically they will "control" the behavior of a dock and their context will be the viewmodels
    //private class AssetExplorer : Tool
    //{

    //}

    //private class EditorContainer : Tool
    //{

    //}

    //private class ProjectExplorer : Tool
    //{

    //}

    #endregion // Docks
}
