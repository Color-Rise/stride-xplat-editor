// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Assets.Editor.GameEditor.ViewModels;
using Stride.Assets.Editor.GameEditor.ContentLoader;

namespace Stride.Assets.Editor.GameEditor.Services;

public interface IEditorGameController : IDestroyable, IDispatcherService
{
    IEditorContentLoader Loader { get; }

    Logger Logger { get; }

    Task GameContentLoaded { get; }

    /// <summary>
    /// The <see cref="SessionNodeContainer"/> to use to create Quantum graphs for game side elements.
    /// </summary>
    /// <remarks>
    /// We use another node container so that nodes created for the game editor can be discarded once we close it.
    /// </remarks>
    AssetNodeContainer GameSideNodeContainer { get; }

    /// <summary>
    /// Starts and initialize the game thread.
    /// </summary>
    /// <returns>True if the game was successfully initialized, false otherwise.</returns>
    Task<bool> StartGame();

    /// <summary>
    /// Creates the scene for the related editor game.
    /// </summary>
    /// <returns>True if the scene was successfully created, false otherwise.</returns>
    Task<bool> CreateScene();

    /// <summary>
    /// Stops the game from rendering and throttle game updates.
    /// </summary>
    void OnHideGame();

    /// <summary>
    /// Resumes the game rendering and updating and .
    /// </summary>
    void OnShowGame();

    /// <summary>
    /// Finds the game-side instance corresponding to the part with the given id, if it exists.
    /// </summary>
    /// <param name="partId">The id of the part to look for.</param>
    /// <returns>The game-side instance corresponding to the given id if it exists, or null.</returns>
    /// <remarks>This method must be called from the game thread.</remarks>
    object? FindGameSidePart(AbsoluteId partId);

    T? GetService<T>() where T : IEditorGameViewModelService;

    /// <summary>
    /// Triggers a re-evaluation of the active render stages.
    /// </summary>
    void TriggerActiveRenderStageReevaluation();
}
