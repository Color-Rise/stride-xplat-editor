// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Presentation.ViewModels;

public interface ICategoryViewModel : IDirtiable, IIsEditableViewModel
{
    string Name { get; }

    IEnumerable Content { get; }
}

public abstract class CategoryViewModel<TChildren> : SessionObjectViewModel, ICategoryViewModel
{
    protected CategoryViewModel(string name, SessionViewModel session, IComparer<TChildren> childComparer = null)
        : base(session)
    {
        Name = name;
        Content = new SortedObservableCollection<TChildren>(childComparer);
    }

    public sealed override string Name { get; set; }

    public SortedObservableCollection<TChildren> Content { get; }

    public override bool IsEditable => false;

    IEnumerable ICategoryViewModel.Content => Content;

    public override string TypeDisplayName => "Category";

    protected override void UpdateIsDeletedStatus()
    {
        if (IsDeleted)
            throw new InvalidOperationException("A category cannot be deleted");
    }
}

public abstract class CategoryViewModel<TParent, TChildren> : CategoryViewModel<TChildren>
{
    protected CategoryViewModel(string name, TParent parent, SessionViewModel session, IComparer<TChildren> childComparer = null)
        : base(name, session, childComparer)
    {
        Parent = parent;
    }

    public TParent Parent { get; }
}
