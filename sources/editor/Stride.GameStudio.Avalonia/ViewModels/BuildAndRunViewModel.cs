// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Execution;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.GameStudio.Avalonia.ViewModels;

internal sealed class BuildAndRunViewModel : DispatcherViewModel
{
    private bool buildIsInProgress;
    private ICancellableAsyncBuild? currentBuild;

    public BuildAndRunViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        BuildProjectCommand = new AnonymousTaskCommand(ServiceProvider, () => BuildProjectAsync(false));
        RunProjectCommand = new AnonymousTaskCommand(ServiceProvider, () => BuildProjectAsync(true));
    }

    /// <summary>
    /// <c>true</c> when a build is currently in progress; otherwise, <c>false</c>.
    /// </summary>
    public bool BuildIsInProgress
    {
        get => buildIsInProgress;
        private set => SetValue(ref buildIsInProgress, value, UpdateCommands);
    }

    public SessionViewModel Session { get; }

    public ICommandBase BuildProjectCommand { get; }

    public ICommandBase RunProjectCommand { get; }

    private async Task<bool> BuildProjectAsync(bool run)
    {
        if (BuildIsInProgress)
            return false;

        try
        {
            BuildIsInProgress = true;
            if (!await PrepareBuild())
                return false;
            
            // FIXME xplat-editor
            //var jobToken = editor.Status.NotifyBackgroundJobStarted("Building...", JobPriority.Compile);
            var result = false;
            try
            {
                return result = await BuildProjectCore(run);
            }
            finally
            {
                // FIXME xplat-editor
                //editor.Status.NotifyBackgroundJobFinished(jobToken);
                //editor.Status.PushDiscardableStatus(result ? "Build successful" : "Build failed");
            }

        }
        finally
        {
            BuildIsInProgress = false;
        }

        async Task<bool> PrepareBuild()
        {
            if (Session.CurrentProject is null)
            {
                await ServiceProvider
                    .Get<IDialogService>()
                    .MessageBoxAsync(
                        Tr._p("Message", "To process the build, set an executable project as the current project in the session explorer."),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // FIXME xplat-editor
            //var saved = await Session.SaveSession();
            var saved = true;
            if (!saved)
            {
                await ServiceProvider
                    .Get<IDialogService>()
                    .MessageBoxAsync(
                        Tr._p("Message", "To build, save the project first."),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            
            // FIXME xplat-editor
            //BuildLog.ClearMessages();
            //BuildLog.ClearLoggers();
            return true;
        }

        async Task<bool> BuildProjectCore(bool run)
        {
            try
            {
                var project = Session.CurrentProject?.Project.Type is ProjectType.Executable ? Session.CurrentProject : null;
                if (project is null)
                {
                    // FIXME xplat-editor
                    //logger.Error(string.Format(Tr._p("Message", "Platform {0} isn't supported for execution."), Session.CurrentProject.Platform != PlatformType.Shared ? Session.CurrentProject.Platform : PlatformType.Windows));
                    return false;
                }

                // Build project
                currentBuild = VSProjectHelper.CompileProjectAssemblyAsync(project.ProjectPath, flags: BuildRequestDataFlags.ProvideProjectStateAfterBuild);
                if (currentBuild is null)
                {
                    // FIXME xplat-editor
                    //logger.Error(string.Format(Tr._p("Message", "Unable to load and compile project {0}"), projectViewModel.ProjectPath));
                    return false;
                }
                
                var assemblyPath = currentBuild.AssemblyPath;
                var buildTask = await currentBuild.BuildTask;

                return currentBuild.IsCanceled != true /*&& !logger.HasErrors*/;
            }
            catch (Exception ex)
            {
                await ServiceProvider
                    .Get<IDialogService>()
                    .MessageBoxAsync(string.Format(
                        Tr._p("Message", "An exception occurred while compiling the project: {0}"), ex.FormatSummary(true)),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
        }
    }

    private void UpdateCommands()
    {
        // TODO xplat-editor update command status
    }
}
