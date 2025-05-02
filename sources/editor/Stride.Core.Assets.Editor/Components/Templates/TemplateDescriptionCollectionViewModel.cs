// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Presentation.Components.Templates;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.Templates;

public abstract class TemplateDescriptionCollectionViewModel : DispatcherViewModel
{
    private string? name;
    private bool userDefinedName;

    private TemplateDescriptionGroupViewModel? selectedGroup;
    private ITemplateDescriptionViewModel? selectedTemplate;
    private readonly ObservableList<ITemplateDescriptionViewModel> templates = [];

    protected TemplateDescriptionCollectionViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public string? Name
    {
        get => name;
        set { userDefinedName = !string.IsNullOrEmpty(value) && name != value; SetValue(ref name, value); }
    }

    public abstract IEnumerable<TemplateDescriptionGroupViewModel> RootGroups { get; }

    public TemplateDescriptionGroupViewModel? SelectedGroup
    {
        get => selectedGroup;
        set => SetValue(ref selectedGroup, value, UpdateTemplateList);
    }

    public ITemplateDescriptionViewModel? SelectedTemplate
    {
        get => selectedTemplate;
        set { SetValue(ref selectedTemplate, value); UpdateName(); }
    }

    public IReadOnlyObservableCollection<ITemplateDescriptionViewModel> Templates => templates;
    
    protected static TemplateDescriptionGroupViewModel? ProcessGroup(TemplateDescriptionGroupViewModel rootGroup, string? groupPath)
    {
        if (string.IsNullOrWhiteSpace(groupPath))
            return null;

        var groupDirectories = groupPath.Split("/\\".ToCharArray());
        return groupDirectories.Aggregate(rootGroup, (current, groupDirectory) => current.SubGroups.FirstOrDefault(x => x.Name == groupDirectory) ?? new TemplateDescriptionGroupViewModel(current, groupDirectory));
    }

    protected abstract string? UpdateNameFromSelectedTemplate();
    
    public abstract bool ValidateProperties([NotNullWhen(false)] out string? error);

    protected void UpdateTemplateList()
    {
        templates.Clear();
        if (SelectedGroup != null)
        {
            templates.AddRange(SelectedGroup.GetTemplatesRecursively());
        }
    }

    private void UpdateName()
    {
        if (userDefinedName)
            return;

        Name = SelectedTemplate is not null ? UpdateNameFromSelectedTemplate() : string.Empty;
        userDefinedName = false;
    }

}
