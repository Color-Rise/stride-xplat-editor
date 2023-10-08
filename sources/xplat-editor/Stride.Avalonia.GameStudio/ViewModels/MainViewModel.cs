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
        public override IRootDock CreateLayout()
        {
            var assetExplorer = new AssetExplorer { Id = nameof(AssetExplorer), Title = nameof(AssetExplorer) };
            var editorContainer = new EditorContainer { Id = nameof(EditorContainer), Title = nameof(EditorContainer) };
            var projectExplorer = new ProjectExplorer { Id = nameof(ProjectExplorer), Title = nameof(ProjectExplorer) };

            var topDock = new ProportionalDock
            {
                Orientation = Orientation.Horizontal,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    new ToolDock
                    {
                        ActiveDockable = editorContainer
                    }
                )
            };
            var bottomDock = new ProportionalDock
            {
                Orientation = Orientation.Horizontal,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    new ToolDock
                    {
                        ActiveDockable = projectExplorer
                    },
                    new ProportionalDockSplitter(),
                    new ToolDock
                    {
                        ActiveDockable = assetExplorer
                    }
                )
            };
            var mainLayout = new ProportionalDock
            {
                Id = "MainLayout",
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>(
                    topDock,
                    new ProportionalDockSplitter(),
                    bottomDock
                )
            };

            var root = CreateRootDock();
            root.Id = "Root";
            root.IsCollapsable = false;
            root.ActiveDockable = mainLayout;
            root.DefaultDockable = mainLayout;

            return root;
        }

        public override void InitLayout(IDockable layout)
        {
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

    // 2023-10-08
    // those are like controls, not view models
    // basically they will "control" the behavior of a dock and their context will be the viewmodels
    private class AssetExplorer : Tool
    {

    }

    private class EditorContainer : Tool
    {
        
    }

    private class ProjectExplorer : Tool
    {
        
    }
}
