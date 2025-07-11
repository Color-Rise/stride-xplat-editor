// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Internal;

namespace Stride.Core.Presentation.Avalonia.Converters;

public sealed class IsNullOrEmpty : OneWayValueConverter<IsNullOrEmpty>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value switch
        {
            null => true,
            string s => string.IsNullOrEmpty(s),
            ICollection<object> c => c.Count == 0,
            IReadOnlyCollection<object> c => c.Count == 0,
            IEnumerable<object> e => !e.Any(),
            _ => false
        }).Box();
    }
}
