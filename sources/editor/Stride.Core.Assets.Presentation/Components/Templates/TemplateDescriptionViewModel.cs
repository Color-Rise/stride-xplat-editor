// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.Components.Templates;

public class TemplateDescriptionViewModel : DispatcherViewModel, ITemplateDescriptionViewModel
{
    public TemplateDescriptionViewModel(IViewModelServiceProvider serviceProvider, TemplateDescription template)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(template);

        Template = template;
    }

    public Guid Id => Template.Id;

    public string Name => Template.Name;

    public string? Description => Template.Description;

    public string? FullDescription => Template.FullDescription;

    public string? DefaultOutputName => Template.DefaultOutputName;

    public UFile? Icon => Template.Icon;

    public List<UFile> Screenshots => Template.Screenshots;

    public TemplateDescription Template { get; }
}
