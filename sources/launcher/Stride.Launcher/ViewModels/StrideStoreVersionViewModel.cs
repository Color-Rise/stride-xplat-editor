// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Packages;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Launcher.Assets.Localization;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// An implementation of the <see cref="StrideVersionViewModel"/> that represents an official release coming from the store.
/// </summary>
public sealed class StrideStoreVersionViewModel : StrideVersionViewModel
{
    public const string PrerequisitesInstaller = @"Bin\Prerequisites\install-prerequisites.exe";

    internal NugetServerPackage LatestServerPackage;
    private ReleaseNotesViewModel releaseNotes;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrideStoreVersionViewModel"/>
    /// </summary>
    /// <param name="launcher"></param>
    /// <param name="store"></param>
    /// <param name="localPackage"></param>
    /// <param name="major"></param>
    /// <param name="minor"></param>
    internal StrideStoreVersionViewModel(MainViewModel launcher, NugetStore store, NugetLocalPackage? localPackage, string packageId, int major, int minor)
        : base(launcher, store, localPackage, packageId, major, minor)
    {
        FetchReleaseNotes();
        FetchDocumentation();
    }

    /// <summary>
    /// Checks whether the latest available package is from a remote repository (i.e. NuGet).
    /// </summary>
    public bool IsLatestPackageRemote
    {
        get
        {
            return (LatestServerPackage?.Source is not null) && Uri.IsWellFormedUriString(LatestServerPackage.Source, UriKind.Absolute);
        }
    }

    /// <summary>
    /// Checks whether the latest available package is from a local repository (i.e. disk).
    /// </summary>
    public bool IsLatestPackageLocal
    {
        get
        {
            return (LatestServerPackage?.Source is not null) && (Directory.Exists(LatestServerPackage.Source));
        }
    }

    /// <summary>
    /// Gets the full name of this version, including revision number and special revision string.
    /// </summary>
    /// <remarks>If this version is installed, it will use the name of the installed version. Otherwise, it will use the name of the latest version available on the server.</remarks>
    public override string FullName
    {
        get
        {
            var result = Version?.ToString() ?? "Unknown";
            //if (ServerPackage is not null)
            //    result += $" ({ServerPackage.Source})";
            return result;
        }
    }

    /// <summary>
    /// Gets the full name of this version on the server.
    /// </summary>
    public string ServerVersionFullName => ServerPackage?.Version?.ToString() ?? "";

    public ObservableList<StrideStoreAlternateVersionViewModel> AlternateVersions { get; } = [];

    /// <summary>
    /// Gets the release notes associated to this version.
    /// </summary>
    public ReleaseNotesViewModel ReleaseNotes { get { return releaseNotes; } private set { SetValue(ref releaseNotes, value); } }

    /// <summary>
    /// Gets the collection of <see cref="DocumentationPageViewModel"/> associated with this version.
    /// </summary>
    public ObservableList<DocumentationPageViewModel> DocumentationPages { get; } = [];

    /// <summary>
    /// Gets the full version of the local package if it exists, or the server package.
    /// </summary>
    /// <value>The version.</value>
    public PackageVersion Version => LocalPackage is not null ? LocalPackage.Version : ServerPackage?.Version;

    /// <summary>
    /// Updates the local package of this version.
    /// </summary>
    /// <param name="package">The local package corresponding to this version.</param>
    internal void UpdateLocalPackage(NugetLocalPackage? package, IEnumerable<NugetLocalPackage>? alternateVersions)
    {
        OnPropertyChanging(nameof(FullName), nameof(Version));
        LocalPackage = package;
        OnPropertyChanged(nameof(FullName), nameof(Version));
        Dispatcher.Invoke(UpdateStatus);
        if (alternateVersions is not null)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateAlternateVersions(alternateVersions, (alternateVersionViewModel, alternateVersion) =>
                {
                    if (alternateVersion is null && alternateVersionViewModel.ServerPackage is null)
                        AlternateVersions.Remove(alternateVersionViewModel);
                    else
                        alternateVersionViewModel.UpdateLocalPackage(alternateVersion);
                });
            });
        }
        Dispatcher.Invoke(UpdateFrameworks);
    }

    /// <summary>
    /// Updates the server package of this version.
    /// </summary>
    /// <param name="package">The server package corresponding to this version.</param>
    internal void UpdateServerPackage(NugetServerPackage package, IEnumerable<NugetServerPackage> alternateVersions)
    {
        OnPropertyChanging(nameof(FullName), nameof(Version));
        ServerPackage = package;
        OnPropertyChanged(nameof(FullName), nameof(Version));

        // Always keep track of highest version
        if (ServerPackage is not null && (LatestServerPackage is null || LatestServerPackage.Version < ServerPackage.Version))
        {
            OnPropertyChanging(nameof(IsLatestPackageRemote), nameof(IsLatestPackageLocal));
            LatestServerPackage = ServerPackage;
            OnPropertyChanged(nameof(IsLatestPackageRemote), nameof(IsLatestPackageLocal));
        }

        Dispatcher.Invoke(UpdateStatus);
        if (alternateVersions is not null)
        {
            Dispatcher.Invoke(() =>
                UpdateAlternateVersions(alternateVersions, (alternateVersionViewModel, alternateVersion) =>
                {
                    if (alternateVersion is null && alternateVersionViewModel.LocalPackage is null)
                        AlternateVersions.Remove(alternateVersionViewModel);
                    else
                        alternateVersionViewModel.UpdateServerPackage(alternateVersion);
                }));
        }
    }

    private void UpdateAlternateVersions<T>(IEnumerable<T> alternateVersions, Action<StrideStoreAlternateVersionViewModel, T> updateAction) where T : NugetPackage
    {
        var updatedViewModels = new HashSet<StrideStoreAlternateVersionViewModel>();
        foreach (var alternateVersion in alternateVersions)
        {

            int index = AlternateVersions.IndexOf(x => x.Version == alternateVersion.Version);
            StrideStoreAlternateVersionViewModel alternateVersionViewModel;
            if (index < 0)
            {
                // If not, add it
                alternateVersionViewModel = new(this);
                AlternateVersions.Add(alternateVersionViewModel);
            }
            else
            {
                // If yes, update it and remove it from the list of old version
                alternateVersionViewModel = AlternateVersions[index];
            }

            updateAction(alternateVersionViewModel, alternateVersion);
            updatedViewModels.Add(alternateVersionViewModel);
        }

        // Update versions that are not installed locally anymore
        foreach (var alternateVersionViewModel in AlternateVersions.Where(x => !updatedViewModels.Contains(x)).ToList())
        {
            updateAction(alternateVersionViewModel, null);
        }
    }

    internal async Task RunPrerequisitesInstaller()
    {
        // Only used for older packages
        if (ServerPackage.Version.Version >= new Version(1, 11, 2, 0))
        {
            return;
        }

        // Run prerequisites installer (if it exists)
        var prerequisitesInstaller = PrerequisitesInstaller;
        var packagePath = Store.GetInstalledPath(ServerPackage.Id, ServerPackage.Version);
        var prerequisitesInstallerPath = Path.Combine(packagePath, prerequisitesInstaller);
        if (File.Exists(prerequisitesInstallerPath))
        {
            CurrentProcessStatus = Strings.ReportInstallingPrerequisites;
            var prerequisitesInstalled = false;
            while (!prerequisitesInstalled)
            {
                try
                {
                    var prerequisitesInstallerProcess = Process.Start(prerequisitesInstallerPath);
                    await prerequisitesInstallerProcess?.WaitForExitAsync();
                    prerequisitesInstalled = true;
                }
                catch
                {
                    // We'll enter this if UAC has been declined, but also if it timed out (which is a frequent case
                    // if you don't stay in front of your computer during the installation.
                    var result = await ServiceProvider.Get<IDialogService>().MessageBoxAsync("The installation of prerequisites has been canceled by user or failed to run. Do you want to run it again?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                        break;
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override string InstallErrorMessage => string.Format(Strings.ErrorInstallingVersion, ServerVersionFullName);

    /// <inheritdoc/>
    protected override string UninstallErrorMessage => string.Format(Strings.ErrorUninstallingVersion, FullName);

    /// <inheritdoc/>
    protected override Task UpdateVersionsFromStore()
    {
        return Launcher.RetrieveAllStrideVersions();
    }

    /// <inheritdoc/>
    protected override void UpdateStatus()
    {
        base.UpdateStatus();
        OnPropertyChanging(nameof(ServerVersionFullName));
        OnPropertyChanged(nameof(ServerVersionFullName));
    }

    /// <inheritdoc/>
    protected override void UpdateInstallStatus()
    {
        switch (CurrentProgressAction)
        {
            case ProgressAction.Download:
                CurrentProcessStatus = string.Format(Strings.ReportDownloadingVersion, ServerVersionFullName, CurrentProgress);
                break;
            case ProgressAction.Install:
                CurrentProcessStatus = string.Format(Strings.ReportInstallingVersion, ServerVersionFullName, CurrentProgress);
                break;
            case ProgressAction.Delete:
                CurrentProcessStatus = string.Format(Strings.ReportDeletingVersion, FullName, CurrentProgress);
                break;
        }
    }

    /// <inheritdoc/>
    protected override void BeforeDownload()
    {
        base.BeforeDownload();
        ReleaseNotes.Show();
    }

    /// <inheritdoc/>
    protected override void AfterDownload()
    {
        base.AfterDownload();
        RunPrerequisitesInstaller().Forget();

        Launcher.ActiveVersion = this;
    }

    /// <summary>
    /// Fetches the documentation pages corresponding to this version from the server.
    /// </summary>
    internal async void FetchDocumentation()
    {
        var pages = await DocumentationPageViewModel.FetchGettingStartedPages(ServiceProvider, $"{Major}.{Minor}");
        await Dispatcher.InvokeAsync(() => { DocumentationPages.Clear(); DocumentationPages.AddRange(pages); });
    }

    internal void FetchReleaseNotes()
    {
        Dispatcher.Invoke(() =>
        {
            ReleaseNotes = new(Launcher, $"{Major}.{Minor}");
            ReleaseNotes.FetchReleaseNotes();
        });
    }
}
