// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Generation;

internal static class GenerationChecksum
{
    internal static int Width(string algorithm) => algorithm switch
    {
        "luhn" or "mod11" => 1,
        "mod97" => 2,
        _ => throw new InvalidSemanticContract("Checksum requires luhn, mod11 or mod97.")
    };

    internal static string? Calculate(string payload, string algorithm)
    {
        _ = Width(algorithm);
        if (!payload.All(character => character is >= ' ' and <= '~')) throw new InvalidSemanticContract("Checksum payload must contain printable ASCII characters.");
        var digits = payload.Where(character => character is >= '0' and <= '9').Select(character => character - '0').ToArray();
        var value = algorithm switch
        {
            "luhn" => Luhn(digits),
            "mod11" => Mod11(digits),
            _ => Mod97(digits)
        };

        return algorithm == "mod11" && value == 10 ? null : value.ToString(algorithm == "mod97" ? "D2" : "D1", CultureInfo.InvariantCulture);
    }

    internal static bool Matches(string value, string algorithm)
    {
        var width = Width(algorithm);
        if (value.Length < width || !value.All(character => character is >= ' ' and <= '~')) return false;
        var suffix = value[^width..];
        if (!suffix.All(character => character is >= '0' and <= '9')) return false;

        return string.Equals(suffix, Calculate(value[..^width], algorithm), StringComparison.Ordinal);
    }

    static int Luhn(int[] digits)
    {
        var sum = 0;
        for (var index = 0; index < digits.Length; index++)
        {
            var digit = digits[digits.Length - index - 1] * (index % 2 == 0 ? 2 : 1);
            sum = (sum + (digit > 9 ? digit - 9 : digit)) % 10;
        }

        return (10 - (sum % 10)) % 10;
    }

    static int Mod11(int[] digits)
    {
        var sum = 0;
        for (var index = 0; index < digits.Length; index++) sum = (sum + (digits[digits.Length - index - 1] * (2 + (index % 6)))) % 11;

        return (11 - (sum % 11)) % 11;
    }

    static int Mod97(int[] digits)
    {
        var residue = 0;
        foreach (var digit in digits) residue = ((residue * 10) + digit) % 97;

        var shifted = residue * 100;
        var remainder = shifted % 97;

        return 98 - remainder;
    }
}
