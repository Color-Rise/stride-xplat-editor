// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public abstract class MainViewModelBase : ViewModelBase, IMainViewModel
{
    private SessionViewModel? session;

    protected MainViewModelBase(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        Status = new StatusViewModel(serviceProvider);
    }
    
    public SessionViewModel? Session
    {
        get => session;
        set
        {
            if (SetValue(ref session, value))
            {
                SessionViewModel.Instance = value!;
            }
        }
    }

    public StatusViewModel Status { get; }

    public abstract Task<bool?> OpenSession(UFile? filePath, CancellationToken token = default);
}
