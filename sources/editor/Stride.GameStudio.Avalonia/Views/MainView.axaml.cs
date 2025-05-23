// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets a platform-specific <see cref="KeyGesture"/> for the Redo action
    /// </summary>
    public static KeyGesture? RedoGesture => Application.Current?.PlatformSettings?.HotkeyConfiguration.Redo.FirstOrDefault();

    /// <summary>
    /// Gets a platform-specific <see cref="KeyGesture"/> for the Undo action
    /// </summary>
    public static KeyGesture? UndoGesture => Application.Current?.PlatformSettings?.HotkeyConfiguration.Undo.FirstOrDefault();
}
