// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.ViewModels;

public readonly struct LogKey : IEquatable<LogKey>
{
    public readonly AssetId AssetId;
    public readonly string Name;

    private LogKey(AssetId assetId, string name)
    {
        AssetId = assetId;
        Name = name;
    }

    public static LogKey Get(string name)
    {
        return new LogKey(AssetId.Empty, name);
    }

    public static LogKey Get(AssetId assetId,  string name)
    {
        if (assetId == AssetId.Empty) throw new ArgumentException(@"The guid cannot be null.", nameof(assetId));
        return new LogKey(assetId, name);
    }

    public static bool operator ==(LogKey left, LogKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LogKey left, LogKey right)
    {
        return !left.Equals(right);
    }

    public bool Equals(LogKey other)
    {
        return AssetId.Equals(other.AssetId) && string.Equals(Name, other.Name);
    }

    public override string ToString()
    {
        return AssetId + Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is LogKey key && Equals(key);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return AssetId.GetHashCode() * 397 ^ (Name != null ? Name.GetHashCode() : 0);
        }
    }
}
