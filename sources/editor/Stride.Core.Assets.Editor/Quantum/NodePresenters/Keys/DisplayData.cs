// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

public static class DisplayData
{
    public const string AttributeDisplayName = nameof(AttributeDisplayName);
    public const string AutoExpandRule = nameof(AutoExpandRule);
    public const string UnloadableObjectInfo = nameof(UnloadableObjectInfo);

    public static readonly PropertyKey<string> AttributeDisplayNameKey = new(AttributeDisplayName, typeof(DisplayData));
    public static readonly PropertyKey<ExpandRule> AutoExpandRuleKey = new(AutoExpandRule, typeof(DisplayData));
}
