// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.GameEditor.ViewModels;
using Stride.Core;

namespace Stride.Assets.Editor.GameEditor.Services;

public interface IEditorGameSelectionViewModelService : IEditorGameViewModelService
{
    bool DisplaySelectionMask { get; set; }

    void AddSelectable(AbsoluteId id);

    void RemoveSelectable(AbsoluteId id);
}
