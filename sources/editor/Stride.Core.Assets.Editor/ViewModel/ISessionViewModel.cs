// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.ViewModels;

public interface ISessionViewModel
{
    ProjectViewModel? CurrentProject { get; }

    AssetPropertyGraphContainer GraphContainer { get; }

    IViewModelServiceProvider ServiceProvider { get; }
    
    /// <summary>
    /// Raised when some assets are modified.
    /// </summary>
    event EventHandler<AssetChangedEventArgs>? AssetPropertiesChanged;

    /// <summary>
    /// Gets an <see cref="AssetViewModel"/> instance of the asset which as the given identifier, if available.
    /// </summary>
    /// <param name="id">The identifier of the asset to look for.</param>
    /// <returns>An <see cref="AssetViewModel"/> that matches the given identifier if available. Otherwise, <c>null</c>.</returns>
    AssetViewModel? GetAssetById(AssetId id);
}
