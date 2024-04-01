// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModel;

public class LoadedPickablePackageViewModel : PickablePackageViewModel
{
    public LoadedPickablePackageViewModel(PackageViewModel package) : base(package.ServiceProvider)
    {
        Package = package;
    }

    public PackageViewModel Package { get; }

    public override string Name => Package.Name;

    public override DependencyRange DependencyRange =>
        (Package.Package.Container is SolutionProject project)
            ? new DependencyRange(Package.Name, new PackageVersionRange(Package.Package.Meta.Version, true), DependencyType.Project)
            {
                MSBuildProject = project.FullPath,
            }
            : new DependencyRange(Package.Name, new PackageVersionRange(Package.Package.Meta.Version, true), DependencyType.Package);
}
