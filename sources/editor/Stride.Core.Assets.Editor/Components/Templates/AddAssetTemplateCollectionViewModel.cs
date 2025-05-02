// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Components.Templates;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.Components.Templates;

public sealed class AddAssetTemplateCollectionViewModel : AddItemTemplateCollectionViewModel
{
    public AddAssetTemplateCollectionViewModel(SessionViewModel session)
        : base(session.SafeArgument().ServiceProvider)
    {
        foreach (TemplateDescription template in TemplateManager.FindTemplates(TemplateScope.Asset, session.PackageSession))
        {
            var group = ProcessGroup(RootGroup, template.Group);
            if (group is not null)
            {
                var viewModel = new TemplateDescriptionViewModel(session.ServiceProvider, template);
                group.Templates.Add(viewModel);
            }
        }

        SelectedGroup = RootGroup;
    }
}
