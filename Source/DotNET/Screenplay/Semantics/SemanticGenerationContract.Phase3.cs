// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Screenplay.Semantics.Generation;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticGenerationContract
{
    internal static bool IsPhase3(SemanticGeneration? generation) => generation is not null && OneOf(generation.Kind, "pattern", "random", "implementation");

    internal static BigInteger? SupportCount(SemanticGeneration? generation, int utcYear)
    {
        if (generation?.Kind == "implementation") return null;
        if (generation?.Kind == "pattern") return GenerationPatternAssessment.CachedMinimumCount(generation.Pattern!, generation.Checksum, utcYear);
        if (generation?.Kind == "random") return new GenerationIntegerRange(generation.Minimum!.Value, generation.Maximum!.Value).Count;
        var source = generation is null ? null : new Syntax.GenerationSyntax(generation.Kind, Diagnostics.SourceLocation.Start)
        {
            Size = generation.Size, Alphabet = generation.Alphabet, Count = generation.Count
        };

        return GenerationEntropy.ExactSupportCount(source, utcYear);
    }

    // Unknown implementation entropy is not a fabricated zero or a known below-threshold count.
    internal static bool HasIdentifierSupport(SemanticGeneration? generation, int utcYear) =>
        SupportCount(generation, utcYear) is not { } count || GenerationEntropy.HasAtLeastBits(count, 64);

    internal static void ValidatePhase3Members(SemanticGeneration generation, int utcYear)
    {
        var pattern = generation.Kind == "pattern";
        var random = generation.Kind == "random";
        var implementation = generation.Kind == "implementation";
        if ((!pattern && (generation.Pattern is not null || generation.Checksum is not null)) ||
            (!random && (generation.Minimum is not null || generation.Maximum is not null)) ||
            (!implementation && generation.RequirementId is not null))
        {
            throw new InvalidSemanticContract("Generation members do not match their strategy.");
        }
        if (!pattern && !random && !implementation) return;
        if (generation.Prefix is not null || generation.Case is not null || generation.Size is not null || generation.Alphabet is not null || generation.Count is not null || generation.Encoding is not null)
        {
            throw new InvalidSemanticContract("Phase-3 generation cannot carry built-in modifiers.");
        }
        if (pattern && generation.Pattern is null) throw new InvalidSemanticContract("Pattern generation requires its pattern.");
        if (random && (generation.Minimum is null || generation.Maximum is null || generation.Minimum > generation.Maximum)) throw new InvalidSemanticContract("Random generation requires ordered inclusive Int64 endpoints.");
        if (implementation && (generation.RequirementId is not { Length: 64 } identity || identity.Any(character => !char.IsAsciiHexDigitLower(character))))
        {
            throw new InvalidSemanticContract("Implementation generation requires a canonical requirement identity.");
        }
        try
        {
            _ = SupportCount(generation, utcYear);
        }
        catch (Exception error) when (error is InvalidGenerationPattern or GenerationSupportBudgetExceeded or EmptyGenerationSupport)
        {
            throw new InvalidSemanticContract(error.Message);
        }
    }
}
