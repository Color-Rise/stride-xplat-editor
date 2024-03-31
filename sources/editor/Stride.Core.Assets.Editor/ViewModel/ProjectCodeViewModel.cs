// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Assets.Presentation.ViewModels;

public class ProjectCodeViewModel : MountPointViewModel
{
    public ProjectCodeViewModel(ProjectViewModel project)
        : base(project)
    {
    }

    public override string Name { get => "Code"; set => throw new NotImplementedException(); }

    public override bool IsEditable => false;

    public override string TypeDisplayName => "Project Code";

    public ProjectViewModel Project => (ProjectViewModel)Package;

    public override bool CanDelete(out string error)
    {
        error = "Projects can't be deleted from Game Studio.";
        return false;
    }

    public override bool AcceptAssetType(Type assetType)
    {
        return typeof(IProjectAsset).IsAssignableFrom(assetType);
    }

    protected override void UpdateIsDeletedStatus()
    {
        throw new NotImplementedException();
    }
}
