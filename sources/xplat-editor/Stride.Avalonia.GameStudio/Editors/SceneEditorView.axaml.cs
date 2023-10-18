// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Stride.Assets.Entities;
using Stride.Avalonia.Core.Assets.Editor.Annotations;

namespace Stride.Avalonia.GameStudio;

// NOTE should we have the class and the XAML in different assemblies? (e.g. 3rd-party)
// if so, does it mean this class doesn't have to know the final type of the editor view model?
// but only that it is a IAssetEditorViewModel<SceneViewModel>

[AssetEditorView<SceneAsset>]
public partial class SceneEditorView : UserControl
{
    public SceneEditorView()
    {
        // TODO create/retrieve the editor viewmodel first (before loading the XAML)
        InitializeComponent();
    }

    public void InitializeComponent(bool loadXaml = true)
    {
        if (loadXaml)
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
