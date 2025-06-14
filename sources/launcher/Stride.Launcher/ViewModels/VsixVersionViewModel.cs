// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Packages;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Launcher.Assets.Localization;

namespace Stride.Launcher.ViewModels;

public sealed class VsixVersionViewModel : PackageVersionViewModel
{
    private readonly string packageId;
    private bool isLatestVersionInstalled;
    private string status;
    private readonly NugetStore.VsixSupportedVsVersion vsixSupportedVsVersion;

    internal VsixVersionViewModel(MainViewModel launcher, NugetStore store, string packageId, NugetStore.VsixSupportedVsVersion vsixSupportedVsVersion)
        : base(launcher, store, null)
    {
        this.packageId = packageId;
        this.vsixSupportedVsVersion = vsixSupportedVsVersion;
        status = FormatStatus(Strings.ReportChecking);
        ExecuteActionCommand = new AnonymousTaskCommand(ServiceProvider, ExecuteAction) { IsEnabled = false };
    }

    /// <inheritdoc/>
    public override string Name => Strings.VisualStudioExtension;

    /// <inheritdoc/>
    public override string FullName => Name;

    /// <summary>
    /// Gets whether the latest version of the VSIX package is installed.
    /// </summary>
    /// <remarks>This property is updated by <see cref="UpdateFromStore"/> and requires the latest Nuget package to be in the local store.</remarks>
    public bool IsLatestVersionInstalled { get { return isLatestVersionInstalled; } private set { SetValue(ref isLatestVersionInstalled, value); } }

    /// <summary>
    /// Gets the current status of the VSIX package.
    /// </summary>
    public string Status { get { return status; } private set { SetValue(ref status, value); } }

    /// <summary>
    /// Gets a command that will download the latest version of the VSIX and install it on all compatible versions of Visual Studio.
    /// </summary>
    public ICommandBase ExecuteActionCommand { get; }

    /// <inheritdoc/>
    protected override string InstallErrorMessage => Strings.ErrorInstallingVSIX;

    /// <inheritdoc/>
    protected override string UninstallErrorMessage => Strings.ErrorUninstallingVSIX;

    public async Task UpdateFromStore()
    {
        Dispatcher.Invoke(() => Status = FormatStatus(Strings.ReportChecking));
        await UpdateVersionsFromStore();
        await Dispatcher.InvokeAsync(UpdateStatus);
    }

    /// <inheritdoc/>
    protected override void UpdateStatus()
    {
        base.UpdateStatus();
        var newStatus = Strings.VSIXVerbReinstall;
        if (CanBeDownloaded)
        {
            newStatus = LocalPackage is null ? Strings.VSIXVerbInstall : Strings.VSIXVerbUpdate;
            IsLatestVersionInstalled = false;
        }

        // Enable the control only if there is an eligible package for the VS extension.
        ExecuteActionCommand.IsEnabled = (LocalPackage is not null || ServerPackage is not null);
        Status = FormatStatus(newStatus);
    }

    private string FormatStatus(string status)
    {
        string vsixTarget = "Visual Studio {0} extension";
        switch (vsixSupportedVsVersion)
        {
            case NugetStore.VsixSupportedVsVersion.VS2019:
                vsixTarget = string.Format(vsixTarget,"2019");
                break;
            case NugetStore.VsixSupportedVsVersion.VS2022:
                vsixTarget = string.Format(vsixTarget, "2022"); ;
                break;
        }
        return $"{vsixTarget}: {status}";
    }

    /// <inheritdoc/>
    protected override void UpdateInstallStatus()
    {
        switch (CurrentProgressAction)
        {
            case ProgressAction.Download:
                CurrentProcessStatus = string.Format(Strings.ReportDownloadingVSIX, CurrentProgress);
                break;
            case ProgressAction.Install:
                CurrentProcessStatus = string.Format(Strings.ReportInstallingVSIX, CurrentProgress);
                break;
            case ProgressAction.Delete:
                CurrentProcessStatus = string.Format(Strings.ReportDeletingVersion, FullName, CurrentProgress);
                break;
        }
    }

    /// <inheritdoc/>
    protected override async Task UpdateVersionsFromStore()
    {
        var versionRange = Store.VsixVersionToStrideRelease[vsixSupportedVsVersion];
        var minVersion = versionRange.MinVersion;
        var maxVersion = versionRange.MaxVersion;

        LocalPackage = await Launcher.RunLockTask(() => Store.GetLocalPackages(packageId).Where(package => package.Version >= minVersion && package.Version < maxVersion).OrderByDescending(p => p.Version).FirstOrDefault());
        ServerPackage = await Launcher.RunLockTask(() => Store.FindSourcePackagesById(packageId, CancellationToken.None).Result.Where(package => package.Version >= minVersion && package.Version < maxVersion).OrderByDescending(p => p.Version).FirstOrDefault());
    }

    public async Task ExecuteAction()
    {
        await Task.Run(async () =>
        {
            await Download(false);

            IsProcessing = true;
            string checkingStatus = Strings.ReportChecking;
            try
            {
                CurrentProcessStatus = checkingStatus;
                IsProcessing = false;
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Strings.VSIXInstallSucessful, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                CurrentProcessStatus = checkingStatus;
                IsProcessing = false;
                var message = $"{Strings.ErrorInstallingVSIX}{e.FormatSummary(true)}";
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UpdateStatus();
        });
    }
}
