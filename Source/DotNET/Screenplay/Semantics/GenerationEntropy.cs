// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using Cratis.Screenplay.Semantics.Generation;
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
    /// Counts the exact distinct support of an authored strategy, with UTC date segments fixed per assessment.
    /// </summary>
    /// <param name="generation">The authored strategy, or null for implicit UUID generation.</param>
    /// <param name="utcAssessmentYear">The injected UTC year; used only for the inclusive date horizon.</param>
    /// <returns>The exact support count, or null for opaque or invalid strategy arguments.</returns>
    /// <remarks>Assessing a parsed strategy does not admit it into an executable semantic model.</remarks>
    public static BigInteger? ExactSupportCount(GenerationSyntax? generation, int utcAssessmentYear)
    {
        var alphabet = generation?.Alphabet ?? SemanticGenerationContract.DefaultAlphabet;
        var size = generation?.Size ?? int.Parse(GenerationStrategyCatalog.OptionFor("nanoid", "size").Default!, CultureInfo.InvariantCulture);
        if (GenerationStrategyCatalog.Find(generation?.Strategy ?? "uuid")?.FixedBits is { } fixedBits) return BigInteger.One << fixedBits;

        return generation?.Strategy switch
        {
            "nanoid" when GenerationStrategyCatalog.InRange(GenerationStrategyCatalog.OptionFor("nanoid", "size"), size) && alphabet.Length >= 2 && alphabet.All(character => character is >= ' ' and <= '~') && alphabet.Distinct().Count() == alphabet.Length => BigInteger.Pow(alphabet.Length, size),
            "bytes" when generation.Count is { } count && GenerationStrategyCatalog.InRange(GenerationStrategyCatalog.OptionFor("bytes", "count"), count) => BigInteger.One << (8 * generation.Count.Value),
            "pattern" when generation.Pattern is { } pattern => GenerationPatternAssessment.CachedMinimumCount(pattern, generation.Checksum, utcAssessmentYear),
            "random" when generation.Range is { } range => GenerationIntegerRange.Parse(range).Count,
            _ => null
        };
    }

    /// <summary>
    /// Calculates display entropy from an exact, positive conditioned support count.
    /// </summary>
    /// <param name="supportCount">The number of distinct admissible values, not repeat decompositions.</param>
    /// <returns>The base-two logarithm, for display and collision estimates only.</returns>
    public static double SupportBits(BigInteger supportCount)
    {
        if (supportCount <= BigInteger.Zero) throw new InvalidSemanticContract("Entropy requires positive generation support.");

        return BigInteger.Log(supportCount, 2);
    }

    /// <summary>
    /// Compares an exact support count to a random-bit threshold without rounding through floating point.
    /// </summary>
    /// <param name="supportCount">The number of distinct admissible values.</param>
    /// <param name="bits">The non-negative bit threshold.</param>
    /// <returns>Whether the support has at least two to the specified power values.</returns>
    public static bool HasAtLeastBits(BigInteger supportCount, int bits)
    {
        if (supportCount <= BigInteger.Zero || bits < 0) throw new InvalidSemanticContract("Entropy thresholds require positive support and non-negative bits.");

        return bits <= supportCount.GetBitLength() && supportCount >= (BigInteger.One << bits);
    }

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
        var length = size ?? int.Parse(GenerationStrategyCatalog.OptionFor("nanoid", "size").Default!, CultureInfo.InvariantCulture);
        double? bits = GenerationStrategyCatalog.Find(strategy ?? "uuid")?.FixedBits ?? (strategy switch
        {
            "nanoid" when length > 0 && alphabet.Length >= 2 && alphabet.Distinct().Count() == alphabet.Length => length * Math.Log2(alphabet.Length),
            "bytes" when count is > 0 => 8d * count.Value,
            _ => (double?)null
        });

        return bits is { } known && double.IsFinite(known) && known >= 0 ? known : null;
    }
}
