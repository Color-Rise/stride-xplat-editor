// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Markup.Xaml;

namespace Stride.Core.Presentation.Avalonia.MarkupExtensions;

/// <summary>
/// This markup extension allows to format the text of a tooltip a text and a gesture.
/// </summary>
public sealed class ToolTipExtension : MarkupExtension
{
    private readonly string content;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolTipExtension"/> class.
    /// </summary>
    /// <param name="text">A string representing the tooltip text</param>
    /// <param name="gesture">A string representing the gesture.</param>
    public ToolTipExtension(string text, string gesture)
    {
        content = !string.IsNullOrEmpty(gesture) ? $"{text} ({gesture})" : text;
    }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return content;
    }
}
