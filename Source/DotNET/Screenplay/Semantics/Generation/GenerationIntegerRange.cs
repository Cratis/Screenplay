// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed record GenerationIntegerRange(long Minimum, long Maximum)
{
    internal BigInteger Count => (BigInteger)Maximum - Minimum + 1;

    internal static GenerationIntegerRange Parse(string range)
    {
        var parts = range.Split("..", StringSplitOptions.None);
        if (parts.Length != 2 || parts.Any(part => !CanonicalDigits(part)) ||
            !long.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var minimum) || !long.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var maximum) || minimum > maximum)
        {
            throw new InvalidSemanticContract("Random generation requires an inclusive, ordered Int64 range.");
        }

        return new(minimum, maximum);
    }

    static bool CanonicalDigits(string text)
    {
        var digits = text.StartsWith('-') ? text[1..] : text;

        return digits.Length > 0 && digits.All(char.IsAsciiDigit) && (digits.Length == 1 || digits[0] != '0');
    }
}
