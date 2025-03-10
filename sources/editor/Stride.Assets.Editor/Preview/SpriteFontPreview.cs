// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.SpriteFont;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;

namespace Stride.Assets.Editor.Preview;

/// <summary>
/// An implementation of the <see cref="AssetPreview"/> that can preview sprite fonts.
/// </summary>
[AssetPreview<SpriteFontAsset>]
public class SpriteFontPreview : FontPreview<SpriteFontAsset>
{
    protected override bool IsFontNotPremultiplied()
    {
        return !Asset.FontType.IsPremultiplied;
    }
}
