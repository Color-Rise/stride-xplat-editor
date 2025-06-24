// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Core.Presentation.Avalonia.Extensions;

namespace Stride.Core.Presentation.Avalonia.Controls;

[TemplatePart(EditableTextBoxPartName, typeof(TextBox), IsRequired = true)]
[TemplatePart(ListBoxPartName, typeof(ListBox), IsRequired = true)]
public sealed class SearchComboBox : SelectingItemsControl
{
    /// <summary>
    /// The name of the part for the <see cref="TextBox"/>.
    /// </summary>
    private const string EditableTextBoxPartName = "PART_EditableTextBox";
    /// <summary>
    /// The name of the part for the <see cref="ListBox"/>.
    /// </summary>
    private const string ListBoxPartName = "PART_ListBox";

    /// <summary>
    /// The input text box.
    /// </summary>
    private TextBox? editableTextBox;
    /// <summary>
    /// The suggestion list box.
    /// </summary>
    private ListBox? listBox;

    /// <summary>
    /// Defines the <see cref="Command"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<Button, ICommand?>(nameof(Command));

    /// <summary>
    /// Defines the <see cref="IsDropDownOpen"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<SearchComboBox, bool>(nameof(IsDropDownOpen));

    /// <summary>
    /// Defines the <see cref="SearchText"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<SearchComboBox, string?>(nameof(SearchText));

    /// <summary>
    /// Defines the <see cref="Watermark"/> property
    /// </summary>
    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<SearchComboBox, string?>(nameof(Watermark));

    static SearchComboBox()
    {
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        editableTextBox = e.NameScope.Find<TextBox>(EditableTextBoxPartName)
            ?? throw new InvalidOperationException($"A part named '{EditableTextBoxPartName}' must be present in the ControlTemplate, and must be of type '{typeof(TextBox)}'.");
        listBox = e.NameScope.Find<ListBox>(ListBoxPartName)
            ?? throw new InvalidOperationException($"A part named '{ListBoxPartName}' must be present in the ControlTemplate, and must be of type '{typeof(ListBox)}'.");

        editableTextBox.KeyDown += EditableTextBoxKeyDown;
        editableTextBox.LostFocus += EditableTextBoxLostFocus;
        return;

        void EditableTextBoxKeyDown(object? sender, KeyEventArgs ev)
        {
            if (ev.Key == Key.Escape)
            {
                Clear();
                return;
            }

            if (listBox.Items.Count <= 0)
            {
                return;
            }

            switch (ev.Key)
            {
                case Key.Up:
                    listBox.SelectedIndex = Math.Max(listBox.SelectedIndex - 1, 0);
                    BringSelectedItemIntoView();
                    break;

                case Key.Down:
                    listBox.SelectedIndex = Math.Min(listBox.SelectedIndex + 1, listBox.Items.Count - 1);
                    BringSelectedItemIntoView();
                    break;

                case Key.PageUp:
                    if (GetListBoxPanel() is { } panel1)
                    {
                        var count = panel1.Children.Count;
                        listBox.SelectedIndex = Math.Max(listBox.SelectedIndex - count, 0);
                    }
                    else
                    {
                        listBox.SelectedIndex = 0;
                    }
                    BringSelectedItemIntoView();
                    break;

                case Key.PageDown:
                    if (GetListBoxPanel() is { } panel2)
                    {
                        var count = panel2.Children.Count;
                        listBox.SelectedIndex = Math.Min(listBox.SelectedIndex + count, listBox.Items.Count - 1);
                    }
                    else
                    {
                        listBox.SelectedIndex = listBox.Items.Count - 1;
                    }
                    BringSelectedItemIntoView();
                    break;

                case Key.Home:
                    listBox.SelectedIndex = 0;
                    BringSelectedItemIntoView();
                    break;

                case Key.End:
                    listBox.SelectedIndex = listBox.Items.Count - 1;
                    BringSelectedItemIntoView();
                    break;
            }
            return;

            void BringSelectedItemIntoView()
            {
                if (listBox.SelectedItem is { } selectedItem)
                {
                    listBox.ScrollIntoView(selectedItem);
                }
            }

            Panel? GetListBoxPanel() => listBox.FindVisualChildOfType<VirtualizingStackPanel>();
        }

        void EditableTextBoxLostFocus(object? sender, RoutedEventArgs _)
        {
            Clear();
        }
    }

    private void Clear()
    {
        editableTextBox!.Text = string.Empty;
        listBox!.SelectedItem = null;
        IsDropDownOpen = false;
    }
}
