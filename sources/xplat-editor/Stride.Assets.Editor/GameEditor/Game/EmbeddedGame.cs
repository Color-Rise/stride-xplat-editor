// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;
using Stride.Core.Diagnostics;
using Stride.Core.BuildEngine;

namespace Stride.Assets.Editor.GameEditor.Game;

public class EmbeddedGame : Engine.Game
{
    public EmbeddedGame()
    {
        GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0, GraphicsProfile.Level_10_1, GraphicsProfile.Level_10_0 };
        GraphicsDeviceManager.PreferredBackBufferWidth = 64;
        GraphicsDeviceManager.PreferredBackBufferHeight = 64;
        GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
        GraphicsDeviceManager.DeviceCreationFlags = DebugMode ? DeviceCreationFlags.Debug : DeviceCreationFlags.None;

        AutoLoadDefaultSettings = false;
    }

    public static bool DebugMode { get; set; }

    /// <inheritdoc />
    protected override void Initialize()
    {
        // Database is needed by effect compiler cache
        MicrothreadLocalDatabases.MountCommonDatabase();

        base.Initialize();

        Window.IsBorderLess = true;
        Window.IsMouseVisible = true;
    }

    /// <inheritdoc />
    protected sealed override LogListener? GetLogListener()
    {
        // We don't want the embedded games to log in the console
        return null;
    }
}
