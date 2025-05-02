// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.Components.Templates;

public sealed class TemplateDescriptionGroupViewModel : DispatcherViewModel, IComparable<TemplateDescriptionGroupViewModel>
{
    public const string Separator = "/";

    private readonly SortedObservableCollection<TemplateDescriptionGroupViewModel> subGroups = [];

    public TemplateDescriptionGroupViewModel(TemplateDescriptionGroupViewModel parent, string name)
        : this(parent.SafeArgument().ServiceProvider, name)
    {
        Parent = parent;
        Parent.subGroups.Add(this);
    }

    public TemplateDescriptionGroupViewModel(IViewModelServiceProvider serviceProvider, string name)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    [MemberNotNullWhen(false, nameof(Parent))]
    public bool IsRoot => Parent is null;

    public string Name { get; }

    public TemplateDescriptionGroupViewModel? Parent { get; }

    public string Path => IsRoot ? "" : Parent.Path + Name + (Name.Length > 0 ? Separator : "");

    public IReadOnlyObservableCollection<TemplateDescriptionGroupViewModel> SubGroups => subGroups;

    public ObservableList<ITemplateDescriptionViewModel> Templates { get; } = [];

    public IEnumerable<ITemplateDescriptionViewModel> GetTemplatesRecursively()
    {
        var allGroup = subGroups.SelectDeep(x => x.subGroups);
        return Templates.Concat(allGroup.SelectMany(x => x.Templates));
    }

    /// <inheritdoc/>
    public int CompareTo(TemplateDescriptionGroupViewModel? other)
    {
        return other is not null ? string.Compare(Name, other.Name, StringComparison.InvariantCultureIgnoreCase) : -1;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"TemplateDescriptionGroupViewModel [{Path}]";
    }
}
