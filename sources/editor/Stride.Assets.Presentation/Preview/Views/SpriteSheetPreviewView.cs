// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Stride.Editor.Annotations;
using Stride.Editor.Preview.View;
using Stride.Editor.Wpf.Preview.Views;

namespace Stride.Assets.Presentation.Preview.Views
{
    [AssetPreviewView<SpriteSheetPreview>]
    public class SpriteSheetPreviewView : StridePreviewView
    {
        static SpriteSheetPreviewView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SpriteSheetPreviewView), new FrameworkPropertyMetadata(typeof(SpriteSheetPreviewView)));
        }
    }
}
