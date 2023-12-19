// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Presentation.ViewModels;

public interface IAssetLogViewModel : IDestroyable
{
    void AddLogger(LogKey key, Logger logger);
}
