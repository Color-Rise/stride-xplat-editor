// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Stride.Core.Assets.Editor.Avalonia.Controls;

[TemplatePart("PART_ListBox", typeof(ListBox))]
[TemplatePart("PART_EditableTextBox", typeof(TextBox))]
public sealed class FilteringBox : SelectingItemsControl
{
    /// <summary>
    /// Defines the <see cref="FiltersSource"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> FiltersSourceProperty =
        AvaloniaProperty.Register<ItemsControl, IEnumerable?>(nameof(FiltersSource));

    /// <summary>
    /// Defines the <see cref="FilterTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> FilterTemplateProperty =
        AvaloniaProperty.Register<ItemsControl, IDataTemplate?>(nameof(FilterTemplate));

    /// <summary>
    /// Defines the <see cref="FilterText"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> FilterTextProperty =
        AvaloniaProperty.Register<FilteringBox, string?>(nameof(FilterText));

    /// <summary>
    /// Defines the <see cref="SelectedFilter"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> SelectedFilterProperty =
        AvaloniaProperty.Register<FilteringBox, object?>(nameof(SelectedFilter));

    private ListBox? listBox;
    private TextBox? textBox;

    public IEnumerable? FiltersSource
    {
        get => GetValue(FiltersSourceProperty);
        set => SetValue(FiltersSourceProperty, value);
    }

    [InheritDataTypeFromItems(nameof(FiltersSource))]
    public IDataTemplate? FilterTemplate
    {
        get => GetValue(FilterTemplateProperty);
        set => SetValue(FilterTemplateProperty, value);
    }

    public string? FilterText
    {
        get => GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    public object? SelectedFilter
    {
        get => GetValue(SelectedFilterProperty);
        set => SetValue(SelectedFilterProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        listBox = e.NameScope.Find<ListBox>("PART_ListBox");
        textBox = e.NameScope.Find<TextBox>("PART_EditableTextBox");
    }
}
