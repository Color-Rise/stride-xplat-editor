// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.IO;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Presentation.ViewModels;

public sealed class DirectoryViewModel : DirectoryBaseViewModel
{
    private string name;
    private DirectoryBaseViewModel parent;

    public DirectoryViewModel(string name, DirectoryBaseViewModel parent)
        : base(parent.Session)
    {
        this.name = name;
        this.parent = parent;
    }

    /// <summary>
    /// Gets whether this directory is editable.
    /// </summary>
    public override bool IsEditable => Package.IsEditable;

    /// <summary>
    /// Gets the package containing this directory.
    /// </summary>
    public override PackageViewModel Package => Parent.Package;

    /// <summary>
    /// Gets or sets the parent directory of this directory.
    /// </summary>
    public override DirectoryBaseViewModel Parent
    {
        get => parent;
        set => SetValue(ref parent, value);
    }

    /// <summary>
    /// Gets or sets the name of this directory.
    /// </summary>
    public override string Name
    {
        get => name;
        set => SetValue(ref name, value);
    }

    /// <summary>
    /// Gets the path of this directory in its current package.
    /// </summary>
    public override string Path => Parent.Path + Name;

    /// <inheritdoc/>
    public override MountPointViewModel Root => Parent.Root;

    /// <inheritdoc/>
    public override string TypeDisplayName => "Folder";

    protected override bool IsValidName(string value, [NotNullWhen(false)] out string? error)
    {
        if (!base.IsValidName(value, out error))
        {
            return false;
        }

        if (Parent.SubDirectories.Any(x => x != this && string.Equals(x.Name, value, StringComparison.InvariantCultureIgnoreCase)))
        {
            error = Tr._p("Message", "A folder with the same name already exists in the parent folder.");
            return false;
        }

        if (!IsPathAcceptedByFilesystem(UPath.Combine<UDirectory>(Package.PackagePath.GetFullDirectory(), value), out error))
        {
            return false;
        }

        return true;

        static bool IsPathAcceptedByFilesystem(string path, out string message)
        {
            message = "";
            var ok = true;
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var normalized = UPath.Normalize(path)!;
                    var result = System.IO.Path.GetFullPath(normalized);
                    if (result.StartsWith("\\\\.\\", StringComparison.Ordinal))
                    {
                        message = Tr._p("Message", "Path is a device name"); // pipe, CON, NUL, COM1...
                        ok = false;
                    }
                }
                catch (Exception e)
                {
                    message = e.Message;
                    ok = false;
                }
            }
            return ok;
        }
    }

    protected override void UpdateIsDeletedStatus()
    {
        throw new NotSupportedException();
    }
}
