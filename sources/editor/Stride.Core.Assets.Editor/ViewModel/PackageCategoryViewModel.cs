// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Presentation.ViewModels;

public class PackageCategoryViewModel : CategoryViewModel<PackageViewModel>, IChildViewModel
{
    public PackageCategoryViewModel(string name, SessionViewModel session, IComparer<PackageViewModel> childComparer = null)
        : base(name, session, childComparer)
    {
    }

    IChildViewModel IChildViewModel.GetParent()
    {
        return null;
    }

    string IChildViewModel.GetName()
    {
        return Name;
    }
}
