namespace Stride.Avalonia.Dock;

internal class DockManager
{
    public Dictionary<string, DockGroup> DockGroups { get; } = new ();

    public DockGroup AddGroup(string groupId)
    {
        var group = new DockGroup { Id = groupId };
        DockGroups.Add(groupId, group);
        return group;
    }

    public bool AddItem(string groupId, DockItem item)
    {
        if (item.Context is IDockGroupFilter filterable && !filterable.MatchGroup(groupId))
            return false;

        if (!DockGroups.TryGetValue(groupId, out var group))
        {
            group = AddGroup(groupId);
        }

        group.Items.Add(item);
        return true;
    }
}
