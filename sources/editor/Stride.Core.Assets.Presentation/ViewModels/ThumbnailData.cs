// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class ThumbnailData : ViewModelBase
{
    private static readonly ConcurrentDictionary<ObjectId, Task<object?>> computingThumbnails = [];
    private object? presenter;

    protected ThumbnailData(ObjectId thumbnailId)
        : base(ViewModelServiceProvider.NullServiceProvider)
    {
        ThumbnailId = thumbnailId;
    }

    public object? Presenter
    {
        get => presenter;
        set => SetValue(ref presenter, value);
    }

    protected ObjectId ThumbnailId { get; }

    public async Task PrepareForPresentation(IDispatcherService dispatcher)
    {
        var task = computingThumbnails.GetOrAdd(ThumbnailId, k => Task.Run(BuildImageSource));

        var result = await task;
        dispatcher.Invoke(() => Presenter = result);
        FreeBuildingResources();

        computingThumbnails.TryRemove(ThumbnailId, out _);
    }

    /// <summary>
    /// Fetches and prepare the image source instance to be displayed.
    /// </summary>
    /// <returns></returns>
    protected abstract object? BuildImageSource();

    /// <summary>
    /// Clears the resources required to build the image source.
    /// </summary>
    protected abstract void FreeBuildingResources();
}
