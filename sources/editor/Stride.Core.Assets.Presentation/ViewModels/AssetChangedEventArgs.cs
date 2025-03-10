// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

/// <summary>
/// Arguments of the  <see cref="ISessionViewModel.AssetPropertiesChanged"/> event.
/// </summary>
public class AssetChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssetChangedEventArgs"/> class.
    /// </summary>
    /// <param name="assets">The collection of assets that have changed.</param>
    public AssetChangedEventArgs(IReadOnlyCollection<AssetViewModel> assets)
    {
        Assets = assets;
    }

    /// <summary>
    /// Gets the collection of assets that have changed.
    /// </summary>
    public IReadOnlyCollection<AssetViewModel> Assets { get; }
}
