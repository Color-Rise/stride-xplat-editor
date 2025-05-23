// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Transactions;

namespace Stride.Core.Presentation.ViewModels;

public abstract class EditableViewModel : DispatcherViewModel
{
    private readonly Dictionary<string, object?> preEditValues = [];
    private readonly HashSet<string> uncancellableChanges = [];
    private readonly List<string> suspendedCollections = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EditableViewModel"/> class.
    /// </summary>
    /// <param name="serviceProvider">A service provider that can provide a <see cref="IDispatcherService"/> and an <see cref="IUndoRedoService"/> to use for this view model.</param>
    protected EditableViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        if (serviceProvider.TryGet<IUndoRedoService>() is null)
            throw new ArgumentException($"The given {nameof(IViewModelServiceProvider)} instance does not contain an service implementing {nameof(IUndoRedoService)}.");
    }

    public abstract IEnumerable<IDirtiable> Dirtiables { get; }

    /// <summary>
    /// Gets the undo/redo service used by this view model.
    /// </summary>
    public IUndoRedoService? UndoRedoService => ServiceProvider.TryGet<IUndoRedoService>();

    protected void RegisterMemberCollectionForActionStack(string name, INotifyCollectionChanged collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        collection.CollectionChanged += (sender, e) => CollectionChanged(sender, e, name);
    }

    protected IDisposable SuspendNotificationForCollectionChange(string name)
    {
        suspendedCollections.Add(name);
        return new AnonymousDisposable(() => suspendedCollections.Remove(name));
    }

    protected bool SetValueUncancellable<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        return SetValueUncancellable(ref field, value, null, [propertyName!]);
    }

    protected bool SetValueUncancellable<T>(ref T field, T value, Action updateAction, [CallerMemberName] string? propertyName = null)
    {
        return SetValueUncancellable(ref field, value, updateAction, [propertyName!]);
    }

    protected bool SetValueUncancellable<T>(ref T field, T value, params string[] propertyNames)
    {
        return SetValueUncancellable(ref field, value, null, propertyNames);
    }

    protected bool SetValueUncancellable<T>(ref T field, T value, Action? updateAction, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            uncancellableChanges.Add(propertyName);
            if (DependentProperties.TryGetValue(propertyName, out var dependentProperties))
            {
                dependentProperties.ForEach(x => uncancellableChanges.Add(x));
            }
        }
        try
        {
            return SetValue(ref field, value, updateAction, false, propertyNames);
        }
        finally
        {
            foreach (var propertyName in propertyNames)
            {
                uncancellableChanges.Remove(propertyName);
                if (DependentProperties.TryGetValue(propertyName, out var dependentProperties))
                {
                    dependentProperties.ForEach(x => uncancellableChanges.Remove(x));
                }
            }
        }
    }

    protected bool SetValueUncancellable(Action? updateAction, [CallerMemberName] string? propertyName = null)
    {
        return SetValueUncancellable(null, updateAction, [propertyName!]);
    }

    protected bool SetValueUncancellable(Action? updateAction, params string[] propertyNames)
    {
        return SetValueUncancellable(null, updateAction, propertyNames);
    }

    protected bool SetValueUncancellable(Func<bool>? hasChangedFunction, Action? updateAction, [CallerMemberName] string? propertyName = null)
    {
        return SetValueUncancellable(hasChangedFunction, updateAction, [propertyName!]);
    }

    protected bool SetValueUncancellable(bool hasChanged, Action? updateAction, [CallerMemberName] string? propertyName = null)
    {
        return SetValueUncancellable(() => hasChanged, updateAction, [propertyName!]);
    }

    protected bool SetValueUncancellable(bool hasChanged, Action? updateAction, params string[] propertyNames)
    {
        return SetValueUncancellable(() => hasChanged, updateAction, propertyNames);
    }

    protected virtual bool SetValueUncancellable(Func<bool>? hasChangedFunction, Action? updateAction, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            uncancellableChanges.Add(propertyName);
            if (DependentProperties.TryGetValue(propertyName, out var dependentProperties))
            {
                dependentProperties.ForEach(x => uncancellableChanges.Add(x));
            }
        }
        try
        {
            return SetValue(hasChangedFunction, updateAction, false, propertyNames);
        }
        finally
        {
            foreach (var propertyName in propertyNames)
            {
                uncancellableChanges.Remove(propertyName);
                if (DependentProperties.TryGetValue(propertyName, out var dependentProperties))
                {
                    dependentProperties.ForEach(x => uncancellableChanges.Remove(x));
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override bool SetValue<T>(ref T field, T value, Action? updateAction, params string[] propertyNames)
    {
        return SetValue(ref field, value, updateAction, true, propertyNames);
    }

    /// <inheritdoc/>
    protected override bool SetValue(Func<bool>? hasChangedFunction, Action? updateAction, params string[] propertyNames)
    {
        return SetValue(hasChangedFunction, updateAction, true, propertyNames);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanging(params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames.Where(x => x != "IsDirty" && !uncancellableChanges.Contains(x)))
        {
            var propertyInfo = GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (propertyInfo?.GetSetMethod() is not null && propertyInfo.GetSetMethod().IsPublic)
            {
                preEditValues.Add(propertyName, propertyInfo.GetValue(this));
            }
        }

        base.OnPropertyChanging(propertyNames);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(params string[] propertyNames)
    {
        base.OnPropertyChanged(propertyNames);

        foreach (string propertyName in propertyNames.Where(x => x != "IsDirty" && !uncancellableChanges.Contains(x)))
        {
            string displayName = $"Update property '{propertyName}'";
            if (preEditValues.TryGetValue(propertyName, out var preEditValue) && !uncancellableChanges.Contains(propertyName))
            {
                var propertyInfo = GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                var postEditValue = propertyInfo?.GetValue(this);
                if (UndoRedoService?.UndoRedoInProgress == false && !Equals(preEditValue, postEditValue))
                {
                    var operation = CreatePropertyChangeOperation(displayName, propertyName, preEditValue);
                    UndoRedoService.PushOperation(operation);
                }
            }
            preEditValues.Remove(propertyName);
        }
    }

    protected virtual Operation CreatePropertyChangeOperation(string displayName, string propertyName, object? preEditValue)
    {
        var operation = new PropertyChangeOperation(propertyName, this, preEditValue, Dirtiables);
        UndoRedoService!.SetName(operation, displayName);
        return operation;
    }

    protected virtual Operation CreateCollectionChangeOperation(string displayName, IList list, NotifyCollectionChangedEventArgs args)
    {
        var operation = new CollectionChangeOperation(list, args, Dirtiables);
        UndoRedoService!.SetName(operation, displayName);
        return operation;
    }

    private bool SetValue<T>(ref T field, T value, Action? updateAction, bool createTransaction, params string[] propertyNames)
    {
        if (propertyNames.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(propertyNames), "This method must be invoked with at least one property name.");

        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            ITransaction? transaction = null;
            if (!UndoRedoService.UndoRedoInProgress && createTransaction)
            {
                transaction = UndoRedoService.CreateTransaction();
                var concatPropertyName = string.Join(", ", propertyNames.Where(x => !uncancellableChanges.Contains(x)).Select(s => $"'{s}'"));
                UndoRedoService.SetName(transaction, $"Update property {concatPropertyName}");
            }
            try
            {
                return base.SetValue(ref field, value, updateAction, propertyNames);
            }
            finally
            {
                if (!UndoRedoService.UndoRedoInProgress && createTransaction)
                {
                    if (transaction is null)
                        throw new InvalidOperationException("A transaction failed to be created.");
                    transaction.Complete();
                }
            }
        }
        return false;
    }

    private bool SetValue(Func<bool>? hasChangedFunction, Action? updateAction, bool createTransaction, params string[] propertyNames)
    {
        if (propertyNames.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(propertyNames), "This method must be invoked with at least one property name.");

        if (hasChangedFunction is null || hasChangedFunction())
        {
            ITransaction? transaction = null;
            if (!UndoRedoService.UndoRedoInProgress && createTransaction)
            {
                transaction = UndoRedoService.CreateTransaction();
                var concatPropertyName = string.Join(", ", propertyNames.Where(x => !uncancellableChanges.Contains(x)).Select(s => $"'{s}'"));
                UndoRedoService.SetName(transaction, $"Update property {concatPropertyName}");
            }
            try
            {
                return base.SetValue(hasChangedFunction, updateAction, propertyNames);
            }
            finally
            {
                if (!UndoRedoService.UndoRedoInProgress && createTransaction)
                {
                    if (transaction is null)
                        throw new InvalidOperationException("A transaction failed to be created.");
                    transaction.Complete();
                }
            }
        }
        return false;
    }

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e, string collectionName)
    {
        string displayName = $"Update collection '{collectionName}' ({e.Action})";
        var list = sender as IList;
        if (list is null)
        {
            var toIListMethod = sender?.GetType().GetMethod("ToIList");
            if (toIListMethod is not null)
                list = (IList?)toIListMethod.Invoke(sender, []);
        }
        if (UndoRedoService?.UndoRedoInProgress == false && !suspendedCollections.Contains(collectionName))
        {
            using (UndoRedoService.CreateTransaction())
            {
                var operation = CreateCollectionChangeOperation(displayName, list, e);
                UndoRedoService.PushOperation(operation);
            }
        }
    }
}
