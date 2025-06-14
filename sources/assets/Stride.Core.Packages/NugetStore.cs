// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using Stride.Core.Extensions;
using Stride.Core.Windows;
using ISettings = NuGet.Configuration.ISettings;
using PackageSource = NuGet.Configuration.PackageSource;
using PackageSourceProvider = NuGet.Configuration.PackageSourceProvider;

namespace Stride.Core.Packages;

/// <summary>
/// Abstraction to interact with a store backed by the NuGet infrastructure.
/// </summary>
public partial class NugetStore : INugetDownloadProgress
{
    private IPackagesLogger? logger;
    private readonly ISettings settings;
    private ProgressReport? currentProgressReport;

    private readonly string? oldRootDirectory;

    private static readonly Regex powerShellProgressRegex = GetPowerShellProgressRegex();

    /// <summary>
    /// Initialize a new instance of <see cref="NugetStore"/>.
    /// </summary>
    /// <param name="oldRootDirectory">The location of the Nuget store.</param>
    public NugetStore(string? oldRootDirectory)
    {
        // Used only for versions before 3.0
        this.oldRootDirectory = oldRootDirectory;

        settings = NuGet.Configuration.Settings.LoadDefaultSettings(null);

        // Remove obsolete sources
        RemoveDeletedSources(settings, "Xenko Dev");
        // Note the space: we want to keep "Stride Dev" but not "Stride Dev {PATH}\bin\packages" anymore
        RemoveSources(settings, "Stride Dev ");

        settings.SaveToDisk();

        InstallPath = SettingsUtility.GetGlobalPackagesFolder(settings);

        var pathContext = NuGetPathContext.Create(settings);
        InstalledPathResolver = new FallbackPackagePathResolver(pathContext.UserPackageFolder, oldRootDirectory != null ? pathContext.FallbackPackageFolders.Concat([oldRootDirectory]) : pathContext.FallbackPackageFolders);
        var packageSourceProvider = new PackageSourceProvider(settings);

        var availableSources = packageSourceProvider.LoadPackageSources().Where(source => source.IsEnabled);
        var packageSources = new List<PackageSource>();
        packageSources.AddRange(availableSources);
        PackageSources = packageSources;

        // Setup source provider as a V3 only.
        sourceRepositoryProvider = new NugetSourceRepositoryProvider(packageSourceProvider, this);
    }

    private static void RemoveSources(ISettings settings, string prefixName)
    {
        var packageSources = settings.GetSection("packageSources");
        if (packageSources != null)
        {
            foreach (var packageSource in packageSources.Items.OfType<SourceItem>().ToList())
            {
                _ = packageSource.GetValueAsPath();
                if (packageSource.Key.StartsWith(prefixName, StringComparison.Ordinal))
                {
                    // Remove entry from packageSources
                    settings.Remove("packageSources", packageSource);
                }
            }
        }
    }

    private static void RemoveDeletedSources(ISettings settings, string prefixName)
    {
        var packageSources = settings.GetSection("packageSources");
        if (packageSources != null)
        {
            foreach (var packageSource in packageSources.Items.OfType<SourceItem>().ToList())
            {
                var path = packageSource.GetValueAsPath();

                if (packageSource.Key.StartsWith(prefixName, StringComparison.Ordinal)
                    && Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsFile // make sure it's a valid file URI
                    && !Directory.Exists(path)) // detect if directory has been deleted
                {
                    // Remove entry from packageSources
                    settings.Remove("packageSources", packageSource);
                }
            }
        }
    }

    public static bool CheckPackageSource(ISettings settings, string name)
    {
        var packageSources = settings.GetSection("packageSources");
        if (packageSources != null)
        {
            foreach (var packageSource in packageSources.Items.OfType<SourceItem>().ToList())
            {
                if (packageSource.Key == name)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static void UpdatePackageSource(ISettings settings, string name, string url)
    {
        settings.AddOrUpdate("packageSources", new SourceItem(name, url));
    }

    private readonly NugetSourceRepositoryProvider sourceRepositoryProvider;

    /// <summary>
    /// Path where all packages are installed.
    /// Usually `InstallPath = RootDirectory/RepositoryPath`.
    /// </summary>
    public string InstallPath { get; }

    /// <summary>
    /// List of package Ids under which the main package is known. Usually just one entry, but
    /// we could have several in case there is a product name change.
    /// </summary>
    // FIXME xplat-editor these names should be parameterized and given to the constructor
    public IReadOnlyCollection<string> MainPackageIds { get; } = ["Stride.GameStudio.Avalonia.Desktop", "Stride.GameStudio", "Xenko.GameStudio", "Xenko"];

    /// <summary>
    /// Package Id of the Visual Studio Integration plugin.
    /// </summary>
    // FIXME xplat-editor this name should be parameterized and given to the constructor
    public string VsixPackageId { get; } = "Stride.VisualStudio.Package";

    /// <summary>
    /// The different supported versions of Visual Studio
    /// </summary>
    public enum VsixSupportedVsVersion
    {
        VS2019,
        VS2022
    }

    /// <summary>
    /// A mapping of the supported versions of VS to a Stride release version range.  
    /// For each supported VS release, the first Version represents the included earliest Stride version eligible for the VSIX and the second Version is the excluded upper bound.
    /// </summary>
    public IReadOnlyDictionary<VsixSupportedVsVersion, (PackageVersion MinVersion, PackageVersion MaxVersion)> VsixVersionToStrideRelease { get; } = new Dictionary<VsixSupportedVsVersion, (PackageVersion, PackageVersion)>
    {
        // The VSIX for VS2019 is avaliable in Stride packages of version 4.0.x
        {VsixSupportedVsVersion.VS2019, (new PackageVersion("4.0"), new PackageVersion("4.1")) },

        // The VSIX for VS2022 is available in Stride packages of version 4.1.x and later.
        {VsixSupportedVsVersion.VS2022, (new PackageVersion("4.1"), new PackageVersion(int.MaxValue,0,0,0)) },
    };

    /// <summary>
    /// Logger for all operations of the package manager.
    /// </summary>
    public IPackagesLogger Logger
    {
        get
        {
            return logger ?? NullPackagesLogger.Instance;
        }

        set
        {
            logger = value;
        }
    }

    private ILogger NativeLogger => new NugetLogger(Logger);

    private IEnumerable<PackageSource> PackageSources { get; }

    /// <summary>
    /// Helper to locate packages.
    /// </summary>
    private FallbackPackagePathResolver InstalledPathResolver { get; }

    /// <summary>
    /// Event executed when a package's installation has completed.
    /// </summary>
    public event EventHandler<PackageOperationEventArgs>? NugetPackageInstalled;

    /// <summary>
    /// Event executed when a package's uninstallation has completed.
    /// </summary>
    public event EventHandler<PackageOperationEventArgs>? NugetPackageUninstalled;

    /// <summary>
    /// Event executed when a package's uninstallation is in progress.
    /// </summary>
    public event EventHandler<PackageOperationEventArgs>? NugetPackageUninstalling;

    /// <summary>
    /// Installation path of <paramref name="package"/>
    /// </summary>
    /// <param name="id">Id of package to query.</param>
    /// <param name="version">Version of package to query.</param>
    /// <returns>The installation path if installed, null otherwise.</returns>
    public string GetInstalledPath(string id, PackageVersion version)
    {
        return InstalledPathResolver.GetPackageDirectory(id, version.ToNuGetVersion());
    }

    /// <summary>
    /// Get the most recent version associated to <paramref name="packageIds"/>. To make sense
    /// it is assumed that packageIds represent the same package under a different name.
    /// </summary>
    /// <param name="packageIds">List of Ids representing a package name.</param>
    /// <returns>The most recent version of `GetPackagesInstalled (packageIds)`.</returns>
    public NugetLocalPackage? GetLatestPackageInstalled(IEnumerable<string> packageIds)
    {
        return GetPackagesInstalled(packageIds).FirstOrDefault();
    }

    /// <summary>
    /// List of all packages represented by <paramref name="packageIds"/>. The list is ordered
    /// from the most recent version to the oldest.
    /// </summary>
    /// <param name="packageIds">List of Ids representing the package names to retrieve.</param>
    /// <returns>The list of packages sorted from the most recent to the oldest.</returns>
    public IList<NugetLocalPackage> GetPackagesInstalled(IEnumerable<string> packageIds)
    {
        return [.. packageIds.SelectMany(GetLocalPackages).OrderByDescending(p => p.Version)];
    }

    /// <summary>
    /// List of all installed packages.
    /// </summary>
    /// <returns>A list of packages.</returns>
    public IEnumerable<NugetLocalPackage> GetLocalPackages(string packageId)
    {
        var res = new List<NugetLocalPackage>();

        // We also scan rootDirectory for 1.x/2.x
        foreach (var installPath in new[] { InstallPath, oldRootDirectory })
        {
            // oldRootDirectory might be null
            if (installPath == null)
                continue;

            var localResource = new FindLocalPackagesResourceV3(installPath);
            var packages = localResource.FindPackagesById(packageId, NativeLogger, CancellationToken.None);
            foreach (var package in packages)
            {
                res.Add(new NugetLocalPackage(package));
            }
        }

        return res;
    }

    /// <summary>
    /// Name of variable used to hold the version of <paramref name="packageId"/>.
    /// </summary>
    /// <param name="packageId">The package Id.</param>
    /// <returns>The name of the variable holding the version of <paramref name="packageId"/>.</returns>
    public static string GetPackageVersionVariable(string packageId, string packageVariablePrefix = "StridePackage")
    {
        ArgumentNullException.ThrowIfNull(packageId);
        var newPackageId = packageId.Replace(".", string.Empty);
        return packageVariablePrefix + newPackageId + "Version";
    }

    /// <summary>
    /// Lock to ensure atomicity of updates to the local repository.
    /// </summary>
    /// <returns>A Lock.</returns>
    private static FileLock? GetLocalRepositoryLock()
    {
        return FileLock.Wait("nuget.lock");
    }

    #region Manager
    /// <summary>
    /// Fetch, if not already downloaded, and install the package represented by
    /// (<paramref name="packageId"/>, <paramref name="version"/>).
    /// </summary>
    /// <remarks>It is safe to call it concurrently be cause we operations are done using the FileLock.</remarks>
    /// <param name="packageId">Name of package to install.</param>
    /// <param name="version">Version of package to install.</param>
    public async Task<NugetLocalPackage?> InstallPackage(string packageId, PackageVersion version, IEnumerable<string> targetFrameworks, ProgressReport progress)
    {
        using (GetLocalRepositoryLock())
        {
            currentProgressReport = progress;
            try
            {
                var identity = new PackageIdentity(packageId, version.ToNuGetVersion());

                var resolutionContext = new ResolutionContext(
                    DependencyBehavior.Lowest,
                    true,
                    true,
                    VersionConstraints.None);

                var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();

                var projectContext = new EmptyNuGetProjectContext()
                {
                    ActionType = NuGetActionType.Install,
                    PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv3, XmlDocFileSaveMode.Skip, null, NativeLogger),
                };

                ActivityCorrelationId.StartNew();

                {
                    var installPath = SettingsUtility.GetGlobalPackagesFolder(settings);

                    // In case it's a package without any TFM (i.e. Visual Studio plugin), we still need to specify one
                    if (!targetFrameworks.Any())
                        targetFrameworks = ["net8.0"];

                    // Old version expects to be installed in GamePackages
                    if (packageId == "Xenko" && version < new PackageVersion(3, 0, 0, 0) && oldRootDirectory != null)
                    {
                        installPath = oldRootDirectory;
                    }

                    var projectPath = Path.Combine("StrideLauncher.json");
                    var spec = new PackageSpec()
                    {
                        Name = Path.GetFileNameWithoutExtension(projectPath), // make sure this package never collides with a dependency
                        FilePath = projectPath,
                        Dependencies =
                        [
                            new()
                            {
                                LibraryRange = new LibraryRange(packageId, new VersionRange(version.ToNuGetVersion()), LibraryDependencyTarget.Package),
                            }
                        ],
                        RestoreMetadata = new ProjectRestoreMetadata
                        {
                            ProjectPath = projectPath,
                            ProjectName = Path.GetFileNameWithoutExtension(projectPath),
                            ProjectStyle = ProjectStyle.PackageReference,
                            ProjectUniqueName = projectPath,
                            OutputPath = Path.Combine(Path.GetTempPath(), $"StrideLauncher-{packageId}-{version}"),
                            OriginalTargetFrameworks = targetFrameworks.ToList(),
                            ConfigFilePaths = settings.GetConfigFilePaths(),
                            PackagesPath = installPath,
                            Sources = SettingsUtility.GetEnabledSources(settings).ToList(),
                            FallbackFolders = [.. SettingsUtility.GetFallbackPackageFolders(settings)]
                        },
                    };
                    foreach (var targetFramework in targetFrameworks)
                    {
                        spec.TargetFrameworks.Add(new TargetFrameworkInformation { FrameworkName = NuGetFramework.Parse(targetFramework) });
                    }

                    using (var context = new SourceCacheContext { MaxAge = DateTimeOffset.UtcNow })
                    {
                        context.IgnoreFailedSources = true;

                        var dependencyGraphSpec = new DependencyGraphSpec();

                        dependencyGraphSpec.AddProject(spec);

                        dependencyGraphSpec.AddRestore(spec.RestoreMetadata.ProjectUniqueName);

                        var requestProvider = new DependencyGraphSpecRequestProvider(new RestoreCommandProvidersCache(), dependencyGraphSpec);
                        var restoreArgs = new RestoreArgs
                        {
                            AllowNoOp = true,
                            CacheContext = context,
                            CachingSourceProvider = new CachingSourceProvider(new PackageSourceProvider(settings)),
                            Log = NativeLogger,
                        };

                        // Create requests from the arguments
                        var requests = requestProvider.CreateRequests(restoreArgs).Result;

                        foreach (var request in requests)
                        {
                            // Limit concurrency to avoid timeout
                            request.Request.MaxDegreeOfConcurrency = 4;

                            var command = new RestoreCommand(request.Request);

                            // Act
                            var result = await command.ExecuteAsync();

                            if (!result.Success)
                            {
                                throw new InvalidOperationException($"Could not restore package {packageId}");
                            }
                            foreach (var install in result.RestoreGraphs.Last().Install)
                            {
                                var package = result.LockFile.Libraries.FirstOrDefault(x => x.Name == install.Library.Name && x.Version == install.Library.Version);
                                if (package != null)
                                {
                                    var packagePath = Path.Combine(installPath, package.Path);
                                    OnPackageInstalled(this, new PackageOperationEventArgs(new PackageName(install.Library.Name, install.Library.Version.ToPackageVersion()), packagePath));
                                }
                            }
                        }
                    }

                    if (packageId == "Xenko" && version < new PackageVersion(3, 0, 0, 0))
                    {
                        UpdateTargetsHelper();
                    }
                }

                // Load the recently installed package
                var installedPackages = GetPackagesInstalled([packageId]);
                return installedPackages.FirstOrDefault(p => p.Version == version);
            }
            finally
            {
                currentProgressReport = null;
            }
        }

        void OnPackageInstalled(object? sender, PackageOperationEventArgs args)
        {
            var packageInstallPath = Path.Combine(args.InstallPath, "tools\\packageinstall.exe");
            if (File.Exists(packageInstallPath))
            {
                RunPackageInstall(packageInstallPath, "/install", currentProgressReport);
            }

            NugetPackageInstalled?.Invoke(sender, args);
        }
    }

    /// <summary>
    /// Uninstall <paramref name="package"/>, while still keeping the downloaded file in the cache.
    /// </summary>
    /// <remarks>It is safe to call it concurrently be cause we operations are done using the FileLock.</remarks>
    /// <param name="package">Package to uninstall.</param>
    public async Task UninstallPackage(NugetPackage package, ProgressReport? progress)
    {
#if DEBUG
        var installedPackages = GetPackagesInstalled([package.Id]);
        Debug.Assert(installedPackages.FirstOrDefault(p => p.Equals(package)) is not null);
#endif
        using (GetLocalRepositoryLock())
        {
            currentProgressReport = progress;
            try
            {
                var identity = new PackageIdentity(package.Id, package.Version.ToNuGetVersion());

                // Notify that uninstallation started.
                var installPath = GetInstalledPath(identity.Id, identity.Version.ToPackageVersion())
                    ?? throw new InvalidOperationException($"Could not find installation path for package {identity}");
                OnPackageUninstalling(this, new PackageOperationEventArgs(new PackageName(package.Id, package.Version), installPath));

                var projectContext = new EmptyNuGetProjectContext()
                {
                    ActionType = NuGetActionType.Uninstall,
                    PackageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv3, XmlDocFileSaveMode.Skip, null, NativeLogger),
                };

                // Simply delete the installed package and its .nupkg installed in it.
                await Task.Run(() => FileSystemUtility.DeleteDirectorySafe(installPath, true, projectContext));

                // Notify that uninstallation completed.
                OnPackageUninstalled(this, new PackageOperationEventArgs(new PackageName(package.Id, package.Version), installPath));
                //currentProgressReport = progress;
                //try
                //{
                //    manager.UninstallPackage(package.IPackage);
                //}
                //finally
                //{
                //    currentProgressReport = null;
                //}

                if (package.Id == "Xenko" && package.Version < new PackageVersion(3, 0, 0, 0))
                {
                    UpdateTargetsHelper();
                }
            }
            finally
            {
                currentProgressReport = null;
            }
        }

        void OnPackageUninstalling(object? sender, PackageOperationEventArgs args)
        {
            NugetPackageUninstalling?.Invoke(sender, args);

            try
            {
                var packageInstallPath = Path.Combine(args.InstallPath, "tools\\packageinstall.exe");
                if (File.Exists(packageInstallPath))
                {
                    RunPackageInstall(packageInstallPath, "/uninstall", currentProgressReport);
                }
            }
            catch (Exception)
            {
                // We mute errors during uninstall since they are usually non-fatal (OTOH, if we don't catch the exception, the NuGet package isn't uninstalled, which is probably not what we want)
                // If we really wanted to deal with them at some point, we should use another mechanism than exception (i.e. log)
            }
        }

        void OnPackageUninstalled(object? sender, PackageOperationEventArgs args)
        {
            NugetPackageUninstalled?.Invoke(sender, args);
        }

    }

    /// <summary>
    /// Find the installed package <paramref name="packageId"/> using the version <paramref name="versionRange"/> if not null, otherwise the <paramref name="constraintProvider"/> if specified.
    /// If no constraints are specified, the first found entry, whatever it means for NuGet, is used.
    /// </summary>
    /// <param name="packageId">Name of the package.</param>
    /// <param name="version">The version range.</param>
    /// <param name="constraintProvider">The package constraint provider.</param>
    /// <param name="allowPrereleaseVersions">if set to <c>true</c> [allow prelease version].</param>
    /// <param name="allowUnlisted">if set to <c>true</c> [allow unlisted].</param>
    /// <returns>A Package matching the search criterion or null if not found.</returns>
    /// <exception cref="ArgumentNullException">packageIdentity</exception>
    /// <returns></returns>
    public NugetPackage? FindLocalPackage(string packageId, PackageVersion? version = null, ConstraintProvider? constraintProvider = null, bool allowPrereleaseVersions = true, bool allowUnlisted = false)
    {
        var versionRange = new PackageVersionRange(version);
        return FindLocalPackage(packageId, versionRange, constraintProvider, allowPrereleaseVersions, allowUnlisted);
    }

    /// <summary>
    /// Find the installed package <paramref name="packageId"/> using the version <paramref name="versionRange"/> if not null, otherwise the <paramref name="constraintProvider"/> if specified.
    /// If no constraints are specified, the first found entry, whatever it means for NuGet, is used.
    /// </summary>
    /// <param name="packageId">Name of the package.</param>
    /// <param name="versionRange">The version range.</param>
    /// <param name="constraintProvider">The package constraint provider.</param>
    /// <param name="allowPrereleaseVersions">if set to <c>true</c> [allow prelease version].</param>
    /// <param name="allowUnlisted">if set to <c>true</c> [allow unlisted].</param>
    /// <returns>A Package matching the search criterion or null if not found.</returns>
    /// <exception cref="ArgumentNullException">packageIdentity</exception>
    /// <returns></returns>
    public NugetLocalPackage? FindLocalPackage(string packageId, PackageVersionRange? versionRange = null, ConstraintProvider? constraintProvider = null, bool allowPrereleaseVersions = true, bool allowUnlisted = false)
    {
        // if an explicit version is specified, disregard the 'allowUnlisted' argument
        // and always allow unlisted packages.
        if (versionRange != null)
        {
            allowUnlisted = true;
        }
        else if (!allowUnlisted && (constraintProvider?.HasConstraints != true))
        {
            // Simple case, we just get the most recent version based on `allowPrereleaseVersions`.
            return GetPackagesInstalled([packageId]).FirstOrDefault(p => allowPrereleaseVersions || string.IsNullOrEmpty(p.Version.SpecialVersion));
        }

        var packages = GetLocalPackages(packageId);

        if (!allowUnlisted)
        {
            packages = packages.Where(p => p.Listed);
        }

        if (constraintProvider != null)
        {
            versionRange = constraintProvider.GetConstraint(packageId) ?? versionRange;
        }
        if (versionRange != null)
        {
            packages = packages.Where(p => versionRange.Contains(p.Version));
        }
        return packages?.FirstOrDefault(p => allowPrereleaseVersions || string.IsNullOrEmpty(p.Version.SpecialVersion));
    }

    /// <summary>
    /// Find available packages from source ith Ids matching <paramref name="packageIds"/>.
    /// </summary>
    /// <param name="packageIds">List of package Ids we are looking for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of packages matching <paramref name="packageIds"/> or an empty list if none is found.</returns>
    public async Task<IEnumerable<NugetServerPackage>> FindSourcePackages(IReadOnlyCollection<string> packageIds, CancellationToken cancellationToken)
    {
        var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();
        var res = new List<NugetServerPackage>();
        foreach (var packageId in packageIds)
        {
            await FindSourcePackagesByIdHelper(packageId, res, repositories, cancellationToken);
        }
        return res;
    }

    /// <summary>
    /// Find available packages from source with Id matching <paramref name="packageId"/>.
    /// </summary>
    /// <param name="packageId">Id of package we are looking for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of packages matching <paramref name="packageId"/> or an empty list if none is found.</returns>
    public async Task<IEnumerable<NugetServerPackage>> FindSourcePackagesById(string packageId, CancellationToken cancellationToken)
    {
        var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();
        var res = new List<NugetServerPackage>();
        await FindSourcePackagesByIdHelper(packageId, res, repositories, cancellationToken);
        return res;
    }

    private async Task FindSourcePackagesByIdHelper(string packageId, List<NugetServerPackage> resultList, SourceRepository[] repositories, CancellationToken cancellationToken)
    {
        using var sourceCacheContext = new SourceCacheContext { MaxAge = DateTimeOffset.UtcNow };
        foreach (var repo in repositories)
        {
            try
            {
                var metadataResource = await repo.GetResourceAsync<PackageMetadataResource>(CancellationToken.None);
                var metadataList = await metadataResource.GetMetadataAsync(packageId, true, true, sourceCacheContext, NativeLogger, cancellationToken);
                foreach (var metadata in metadataList)
                {
                    if (metadata.IsListed)
                        resultList.Add(new NugetServerPackage(metadata, repo.PackageSource.Source));
                }
            }
            catch (FatalProtocolException)
            {
                // Ignore 404/403 etc... (invalid sources)
            }
        }
    }

    /// <summary>
    /// Look for available packages from source containing <paramref name="searchTerm"/> in either the Id or description of the package.
    /// </summary>
    /// <param name="searchTerm">Term used for search.</param>
    /// <param name="allowPrereleaseVersions">Are we looking in pre-release versions too?</param>
    /// <returns>A list of packages matching <paramref name="searchTerm"/>.</returns>
    public async Task<IQueryable<NugetPackage>> SourceSearch(string searchTerm, bool allowPrereleaseVersions)
    {
        var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();
        var res = new List<NugetPackage>();
        foreach (var repo in repositories)
        {
            try
            {
                var searchResource = await repo.GetResourceAsync<PackageSearchResource>(CancellationToken.None);

                if (searchResource != null)
                {
                    var searchResults = await searchResource.SearchAsync(searchTerm, new SearchFilter(includePrerelease: false), 0, 0, NativeLogger, CancellationToken.None);

                    if (searchResults != null)
                    {
                        var packages = searchResults.ToArray();

                        foreach (var package in packages)
                        {
                            if (package.IsListed)
                                res.Add(new NugetServerPackage(package, repo.PackageSource.Source));
                        }
                    }
                }
            }
            catch (FatalProtocolException)
            {
                // Ignore 404/403 etc... (invalid sources)
            }
        }
        return res.AsQueryable();
    }

    /// <summary>
    /// Returns updates for packages from the repository
    /// </summary>
    /// <param name="packageName">Package to look for updates</param>
    /// <param name="includePrerelease">Indicates whether to consider prerelease updates.</param>
    /// <param name="includeAllVersions">Indicates whether to include all versions of an update as opposed to only including the latest version.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    public async Task<IEnumerable<NugetPackage>> GetUpdates(PackageName packageName, bool includePrerelease, bool includeAllVersions, CancellationToken cancellationToken)
    {
        var resolutionContext = new ResolutionContext(
           DependencyBehavior.Lowest,
           includePrerelease,
           true,
           includeAllVersions ? VersionConstraints.None : VersionConstraints.ExactMajor | VersionConstraints.ExactMinor);

        var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();

        var res = new List<NugetPackage>();
        using (var context = new SourceCacheContext { MaxAge = DateTimeOffset.UtcNow })
        {
            foreach (var repo in repositories)
            {
                try
                {
                    var metadataResource = await repo.GetResourceAsync<PackageMetadataResource>(cancellationToken);
                    var metadataList = await metadataResource.GetMetadataAsync(packageName.Id, includePrerelease, includeAllVersions, context, NativeLogger, cancellationToken);
                    foreach (var metadata in metadataList)
                    {
                        if (metadata.IsListed)
                            res.Add(new NugetServerPackage(metadata, repo.PackageSource.Source));
                    }
                }
                catch (FatalProtocolException)
                {
                    // Ignore 404/403 etc... (invalid sources)
                }
            }
        }
        return res;
    }
    #endregion


    /// <summary>
    /// Clean all temporary files created thus far during store operations.
    /// </summary>
    public void PurgeCache()
    {
    }

    public string GetRealPath(NugetLocalPackage package)
    {
        if (IsDevRedirectPackage(package) && package.Version < new PackageVersion(3, 1, 0, 0))
        {
            var realPath = File.ReadAllText(GetRedirectFile(package));
            if (!Directory.Exists(realPath))
                throw new DirectoryNotFoundException();
            return realPath;
        }

        return package.Path;
    }

    public string GetRedirectFile(NugetLocalPackage package)
    {
        return Path.Combine(package.Path, $"{package.Id}.redirect");
    }

    public bool IsDevRedirectPackage(NugetLocalPackage package)
    {
        return package.Version < new PackageVersion(3, 1, 0, 0)
            ? File.Exists(GetRedirectFile(package))
            : (package.Version.SpecialVersion?.StartsWith("dev", StringComparison.Ordinal) == true && !package.Version.SpecialVersion.Contains('.'));
    }

    public bool IsDevRedirectPackage(NugetServerPackage package)
    {
        return package.Version.SpecialVersion?.StartsWith("dev", StringComparison.Ordinal) == true && !package.Version.SpecialVersion.Contains('.');
    }

    void INugetDownloadProgress.DownloadProgress(long contentPosition, long contentLength)
    {
        currentProgressReport?.UpdateProgress(ProgressAction.Download, (int)(contentPosition * 100 / contentLength));
    }

    private static void RunPackageInstall(string packageInstall, string arguments, ProgressReport progress)
    {
        // Run packageinstall.exe
        using var process = Process.Start(new ProcessStartInfo(packageInstall, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(packageInstall),
        }) ?? throw new InvalidOperationException($"Could not start install package process [{packageInstall}] with options {arguments}");
        var errorOutput = new StringBuilder();

        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                var matches = powerShellProgressRegex.Match(args.Data);
                if (matches.Success && int.TryParse(matches.Groups[1].Value, out var percentageResult))
                {
                    // Report progress
                    progress?.UpdateProgress(ProgressAction.Install, percentageResult);
                }
                else
                {
                    lock (process)
                    {
                        errorOutput.AppendLine(args.Data);
                    }
                }
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                // Save errors
                lock (process)
                {
                    errorOutput.AppendLine(args.Data);
                }
            }
        };

        // Process output and wait for exit
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        // Check exit code
        var exitCode = process.ExitCode;
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Error code {exitCode} while running install package process [{packageInstall}]\n\n" + errorOutput);
        }
    }

    // Used only for Stride 1.x and 2.x
    private void UpdateTargetsHelper()
    {
        if (oldRootDirectory == null)
            return;

        // Get latest package only for each MainPackageIds (up to 2.x)
        var strideOldPackages = GetLocalPackages("Xenko").Where(package => package.Tags?.Contains("internal") != true).Where(x => x.Version.Version.Major < 3).ToList();

        // Generate target file
        var targetGenerator = new TargetGenerator(this, strideOldPackages, "SiliconStudioPackage");
        var targetFileContent = targetGenerator.TransformText();
        var targetFile = Path.Combine(oldRootDirectory, @"Targets\SiliconStudio.Common.targets");

        var targetFilePath = Path.GetDirectoryName(targetFile);

        // Make sure directory exists
        if (targetFilePath != null && !Directory.Exists(targetFilePath))
            Directory.CreateDirectory(targetFilePath);

        File.WriteAllText(targetFile, targetFileContent, Encoding.UTF8);
    }

    private class PackagePathResolverV3 : PackagePathResolver
    {
        private readonly VersionFolderPathResolver pathResolver;

        public PackagePathResolverV3(string rootDirectory) : base(rootDirectory, true)
        {
            pathResolver = new VersionFolderPathResolver(rootDirectory);
        }

        public override string GetPackageDirectoryName(PackageIdentity packageIdentity)
        {
            return pathResolver.GetPackageDirectory(packageIdentity.Id, packageIdentity.Version);
        }

        public override string GetPackageFileName(PackageIdentity packageIdentity)
        {
            return pathResolver.GetPackageFileName(packageIdentity.Id, packageIdentity.Version);
        }

        public override string GetInstallPath(PackageIdentity packageIdentity)
        {
            return pathResolver.GetInstallPath(packageIdentity.Id, packageIdentity.Version);
        }

        public override string? GetInstalledPath(PackageIdentity packageIdentity)
        {
            var installPath = GetInstallPath(packageIdentity);
            var installPackagePath = GetInstalledPackageFilePath(packageIdentity);
            return File.Exists(installPackagePath) ? installPath : null;
        }

        public override string? GetInstalledPackageFilePath(PackageIdentity packageIdentity)
        {
            var installPath = GetInstallPath(packageIdentity);
            var installPackagePath = Path.Combine(installPath, packageIdentity.ToString().ToLower() + PackagingCoreConstants.NupkgExtension);
            return File.Exists(installPackagePath) ? installPackagePath : null;
        }
    }

    [GeneratedRegex(@"\[ProgressReport:\s*(\d*)%\]")]
    private static partial Regex GetPowerShellProgressRegex();
}
