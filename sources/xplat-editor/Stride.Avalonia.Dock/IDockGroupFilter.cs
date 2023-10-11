namespace Stride.Avalonia.Dock;

internal interface IDockGroupFilter
{
    bool MatchGroup(string groupId);
}
