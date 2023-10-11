namespace Stride.Avalonia.Dock;

internal class DockGroup
{
    public required string Id { get; init; }

    public List<DockItem> Items { get; } = new ();
}
