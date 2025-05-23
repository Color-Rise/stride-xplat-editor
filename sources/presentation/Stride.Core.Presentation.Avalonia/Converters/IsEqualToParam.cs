// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Internal;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This converter will convert an object to a boolean. If the given value is equal (or reference-equal for non-value type) to the parameter, it will
/// return <c>true</c>. Otherwise, it will return <c>false</c>.
/// </summary>
public sealed class IsEqualToParam : OneWayValueConverter<IsEqualToParam>
{
    /// <inheritdoc/>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return parameter is null;

        var useEquals = value.GetType().IsValueType || value is string;
        var result = useEquals ? Equals(value, parameter) : ReferenceEquals(value, parameter);
        return result.Box();
    }
}
