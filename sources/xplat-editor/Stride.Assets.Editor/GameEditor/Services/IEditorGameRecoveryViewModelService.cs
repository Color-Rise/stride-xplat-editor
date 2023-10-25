// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.Editor.GameEditor.ViewModels;

namespace Stride.Assets.Editor.GameEditor.Services;

/// <summary>
/// React to editor game exceptions.
/// </summary>
public interface IEditorGameRecoveryViewModelService : IEditorGameViewModelService
{
    /// <summary>
    /// Resume a faulted editor game.
    /// </summary>
    void Resume();
}
