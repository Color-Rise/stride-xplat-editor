// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

/// <summary>
/// Represents a group of <see cref="AbstractNodeType"/>.
/// </summary>
public sealed class AbstractNodeTypeGroup : AbstractNodeEntry
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="types"></param>
    /// <exception cref="ArgumentNullException">If groupName or types is null or empty.</exception>
    public AbstractNodeTypeGroup(string groupName, IEnumerable<AbstractNodeType> types)
    {
        if (types.IsNullOrEmpty())
        {
            throw new ArgumentNullException(nameof(types));
        }

        GroupName = groupName;
        Types = types;
    }

    /// <summary>
    /// The group name;
    /// </summary>
    public string GroupName { get; }

    /// <summary>
    /// The <see cref="AbstractNodeType"/>s in this group.
    /// </summary>
    public IEnumerable<AbstractNodeType> Types { get; }

    /// <inheritdoc/>
    public override int Order => 0;

    /// <inheritdoc/>
    public override string DisplayValue => GroupName;

    /// <inheritdoc/>
    public override bool Equals(AbstractNodeEntry? other)
    {
        if (other is AbstractNodeTypeGroup otherGroup)
        {
            return GroupName == otherGroup.GroupName && Types.SequenceEqual(otherGroup.Types);
        }

        return false;
    }

    /// <inheritdoc/>
    public override object? GenerateValue(object? currentValue) => null;

    /// <inheritdoc/>
    public override bool IsMatchingValue(object? value) => false;

    protected override int ComputeHashCode()
    {
        //Not sure about this
        return GroupName.GetHashCode() ^ Types.ToHashCode();
    }
}
