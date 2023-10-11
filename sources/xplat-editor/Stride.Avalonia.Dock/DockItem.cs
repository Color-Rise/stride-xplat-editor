namespace Stride.Avalonia.Dock;

internal class DockItem
{
    public required string Id { get; init; }

    public IDockable? Context { get; set; }
}
