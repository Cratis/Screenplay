// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed record GenerationPatternToken(string Characters, int Minimum = 1, int Maximum = 1, string? Segment = null)
{
    internal int Width => Segment switch { "yyyy" => 4, null => 1, _ => 2 };

    internal string[] Expand(DateOnly? date)
    {
        if (Segment is null) return [Characters];
        if (date is null) return [.. Enumerable.Repeat("0123456789", Width)];
        var text = date.Value.ToString(Segment, CultureInfo.InvariantCulture);

        return [.. text.Select(character => character.ToString())];
    }
}
