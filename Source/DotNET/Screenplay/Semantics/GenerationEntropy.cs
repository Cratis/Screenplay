// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Calculates random entropy and birthday collision estimates for declared generation strategies.
/// Prefixes, letter case and encoding add no randomness. Preview strategies have unknown entropy.
/// </summary>
public static class GenerationEntropy
{
    /// <summary>
    /// Calculates the exact number of random bits, including the implicit UUID default.
    /// </summary>
    /// <param name="generation">The authored strategy, or null for implicit UUID generation.</param>
    /// <returns>The finite, non-negative random bits, or null when the strategy or its inputs have unknown entropy.</returns>
    public static double? Bits(GenerationSyntax? generation) => Bits(generation?.Strategy, generation?.Size, generation?.Alphabet, generation?.Count);

    /// <summary>
    /// Estimates how many values reach one percent birthday collision probability.
    /// </summary>
    /// <param name="bits">The random entropy in bits.</param>
    /// <returns>The birthday estimate, or positive infinity when it exceeds double precision's range.</returns>
    public static double ValuesAtOnePercentCollision(double bits) => Math.Sqrt(2 * Math.Pow(2, bits) * Math.Log(1 / 0.99));

    /// <summary>
    /// Formats the collision estimate as an invariant JSON number, including estimates beyond double range.
    /// </summary>
    /// <param name="bits">The random entropy in bits.</param>
    /// <returns>A finite decimal or scientific number suitable for JSON numeric transport.</returns>
    public static string CollisionCountText(double bits)
    {
        var count = ValuesAtOnePercentCollision(bits);
        if (double.IsFinite(count)) return count.ToString("R", CultureInfo.InvariantCulture);
        var logarithm = ((bits * Math.Log10(2)) + Math.Log10(2 * Math.Log(1 / 0.99))) / 2;
        var exponent = Math.Floor(logarithm);
        var mantissa = Math.Pow(10, logarithm - exponent);

        return string.Create(CultureInfo.InvariantCulture, $"{mantissa:R}e{exponent:0}");
    }

    internal static double? Bits(string? strategy, int? size, string? alphabet, int? count)
    {
        alphabet ??= SemanticGenerationContract.DefaultAlphabet;
        var length = size ?? 21;
        double? bits = strategy switch
        {
            null or "uuid" => 122,
            "uuidv7" or "typeid" => 74,
            "ulid" => 80,
            "nanoid" when length > 0 && alphabet.Length >= 2 && alphabet.Distinct().Count() == alphabet.Length => length * Math.Log2(alphabet.Length),
            "bytes" when count is > 0 => 8d * count.Value,
            _ => null
        };

        return bits is { } known && double.IsFinite(known) && known >= 0 ? known : null;
    }
}
