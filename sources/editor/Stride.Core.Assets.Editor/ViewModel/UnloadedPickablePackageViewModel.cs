// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Packages;

namespace Stride.Core.Assets.Editor.ViewModel;

public class UnloadedPickablePackageViewModel : PickablePackageViewModel
{
    private readonly SessionViewModel session;
    private readonly PackageName package;

    public UnloadedPickablePackageViewModel(SessionViewModel session, PackageName package) : base(session.ServiceProvider)
    {
        this.session = session;
        this.package = package;
    }

    public override string Name => package.Id;

    public override DependencyRange DependencyRange =>
        new(package.Id, new PackageVersionRange(package.Version, true), DependencyType.Package);
}
