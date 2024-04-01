// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModel;

public abstract class PickablePackageViewModel : DispatcherViewModel
{
    protected PickablePackageViewModel([NotNull] IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public abstract string Name { get; }

    public abstract DependencyRange DependencyRange { get; }
}
