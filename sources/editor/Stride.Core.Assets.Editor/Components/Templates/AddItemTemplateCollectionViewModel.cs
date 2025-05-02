// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Presentation.Components.Templates;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.Templates;

public abstract class AddItemTemplateCollectionViewModel : TemplateDescriptionCollectionViewModel
{
    private string? searchToken;

    protected AddItemTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        RootGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "All templates");
    }

    public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups => RootGroup.Yield()!;

    public TemplateDescriptionGroupViewModel RootGroup { get; }

    public string? SearchToken
    {
        get => searchToken;
        set => SetValue(ref searchToken, value, () => SelectedGroup = RootGroup);
    }

    public override bool ValidateProperties([NotNullWhen(false)] out string? error)
    {
        error = null;
        return true;
    }

    protected override string? UpdateNameFromSelectedTemplate()
    {
        return null;
    }
}
