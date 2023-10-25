// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.GameEditor.Services;

namespace Stride.Assets.Editor.GameEditor.Game;

public class EditorServiceGame : EmbeddedGame
{
    public EditorGameServiceRegistry? EditorServices { get; private set; }
    
    public void RegisterServices(EditorGameServiceRegistry serviceRegistry)
    {
        EditorServices = serviceRegistry;
    }
}
