// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services;

public interface IEditorDebugService
{    
    ITitledPage? CreateLogDebugPage(Logger logger, string title, bool register = true);

    ITitledPage? CreateUndoRedoDebugPage(IUndoRedoService actionService, string title, bool register = true);

    ITitledPage? CreateAssetNodesDebugPage(ISessionViewModel session, string title, bool register = true);

    void RegisterDebugPage(ITitledPage? page);

    void UnregisterDebugPage(ITitledPage? page);
}
