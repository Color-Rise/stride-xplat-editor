using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
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

        AboutCommand = new AsyncRelayCommand(OnAbout, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        ExitCommand = new RelayCommand(OnExit, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        OpenCommand = new RelayCommand(OnOpen);
    }

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

    private class DockFactory : Factory
    {
        private IRootDock? _rootDock;

        private IProportionalDock? _mainLayout;
        private IProportionalDock? _topDock;
        private IProportionalDock? _bottomDock;

        private IDocumentDock? _documentDock;

        private IToolDock? _assetExplorer;
        private IToolDock? _assetPreview;
        private IToolDock? _projectExplorer;
        private IToolDock? _propertyGrid;

        public override IRootDock CreateLayout()
        {
            _topDock = new ProportionalDock
            {
                Id = "Top",
                Orientation = Orientation.Horizontal,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    _documentDock = new DocumentDock
                    {
                        Id = "Editors"
                    },
                    new ProportionalDockSplitter(),
                    _propertyGrid = new ToolDock
                    {
                        Id = "PropertyGrid"
                    }
                )
            };
            _bottomDock = new ProportionalDock
            {
                Id = "Bottom",
                Orientation = Orientation.Horizontal,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    _projectExplorer = new ToolDock
                    {
                        Id = "ProjectExplorer"
                    },
                    new ProportionalDockSplitter(),
                    _assetExplorer = new ToolDock
                    {
                        Id = "AssetExplorer",
                    },
                    new ProportionalDockSplitter(),
                    _assetPreview = new ToolDock
                    {
                        Id = "AssetPreview",
                    }
                )
            };
            _mainLayout = new ProportionalDock
            {
                Id = "Main",
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>(
                    _topDock,
                    new ProportionalDockSplitter(),
                    _bottomDock
                )
            };

            _rootDock = CreateRootDock();
            _rootDock.Id = "Root";
            _rootDock.IsCollapsable = false;
            _rootDock.ActiveDockable = _mainLayout;
            _rootDock.DefaultDockable = _mainLayout;

            return _rootDock;
        }

        public override void InitLayout(IDockable layout)
        {
            DockableLocator = new Dictionary<string, Func<IDockable?>>
            {
                ["Root"] = () => _rootDock,
                ["Main"] = () => _mainLayout,
                ["Top"] = () => _mainLayout,
                ["Bottom"] = () => _mainLayout,
                ["Editors"] = () => _documentDock,
                ["AssetExplorer"] = () => _assetExplorer,
                ["AssetPreview"] = () => _assetPreview,
                ["ProjectExplorer"] = () => _projectExplorer,
                ["PropertyGrid"] = () => _propertyGrid,
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
}
