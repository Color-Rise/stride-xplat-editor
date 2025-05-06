// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class SessionObjectViewModel : DirtiableEditableViewModel, IIsEditableViewModel
{
    private bool isEditing;
    private bool isDeleted = true;

    protected SessionObjectViewModel(ISessionViewModel session)
        : base(session.ServiceProvider)
    {
        Session = session;
    }

    /// <summary>
    /// Gets whether this object is currently deleted.
    /// </summary>
    public bool IsDeleted { get { return isDeleted; } set { SetValue(ref isDeleted, value, UpdateIsDeletedStatus); } }

    /// <summary>
    /// Gets whether this object is editable.
    /// </summary>
    public abstract bool IsEditable { get; }

    /// <summary>
    /// Gets or sets whether this package is being edited in the view.
    /// </summary>
    public virtual bool IsEditing { get { return isEditing; } set { if (IsEditable) SetValueUncancellable(ref isEditing, value); } }

    public abstract string Name { get; set; }

    /// <summary>
    /// Gets the session in which this object is currently in.
    /// </summary>
    public ISessionViewModel Session { get; }

    /// <summary>
    /// Gets the display name of the type of this <see cref="SessionObjectViewModel"/>.
    /// </summary>
    public abstract string TypeDisplayName { get; }

    /// <summary>
    /// Marks this view model as undeleted.
    /// </summary>
    /// <param name="canUndoRedoCreation">Indicates whether a transaction should be created when doing this operation.</param>
    /// <remarks>
    /// This method is intended to be called from constructors, to allow the creation of this view model
    /// to be undoable or not.
    /// </remarks>
    protected void InitialUndelete(bool canUndoRedoCreation)
    {
        if (canUndoRedoCreation)
        {
            SetValue(ref isDeleted, false, UpdateIsDeletedStatus, nameof(IsDeleted));
        }
        else
        {
            SetValueUncancellable(ref isDeleted, false, UpdateIsDeletedStatus, nameof(IsDeleted));
        }
    }

    protected virtual bool IsValidName(string value, [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(value);

        // FIXME xplat-editor limits might be different per OS
        if (value.Length > 240)
        {
            error = Tr._p("Message", "The name is too long.");
            return false;
        }

        if (value.Contains(UPath.DirectorySeparatorChar) || value.Contains(UPath.DirectorySeparatorCharAlt) || !UPath.IsValid(value))
        {
            error = Tr._p("Message", "The name contains invalid characters.");
            return false;
        }

        if (string.IsNullOrEmpty(value))
        {
            error = Tr._p("Message", "The name is empty.");
            return false;
        }

        error = null;

        return true;
    }

    /// <summary>
    /// Updates related session objects when the <see cref="IsDeleted"/> property changes.
    /// </summary>
    protected abstract void UpdateIsDeletedStatus();
}
