// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticGenerationContract
{
    internal const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";

    internal static double? Bits(SemanticGeneration? generation) =>
        GenerationEntropy.Bits(generation?.Kind, generation?.Size, generation?.Alphabet, generation?.Count);

    internal static void Validate(SemanticConcept concept, int? utcAssessmentYear = null)
    {
        if (concept.Generation is not { } generation) return;
        var kind = generation.Kind;
        if (!OneOf(kind, "uuid", "uuidv7", "ulid", "typeid", "nanoid", "bytes", "pattern", "random", "implementation") || !concept.Values.IsEmpty ||
            (kind == "random" ? concept.Primitive != SemanticPrimitiveType.WholeNumber : concept.Primitive is not (SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid)) ||
            (concept.Primitive == SemanticPrimitiveType.Uuid && !OneOf(kind, "uuid", "uuidv7")))
        {
            throw new InvalidSemanticContract("Generation requires an admitted strategy and a compatible scalar concept primitive.");
        }

        if (concept.Compliance is not null)
        {
            throw new InvalidSemanticContract("Generation does not admit compliance markers until #616 phase 4.");
        }

        ValidatePhase3Members(generation, utcAssessmentYear ?? DateTimeOffset.UtcNow.Year);
        if (IsPhase3(generation)) return;
        ValidateModifiers(concept, generation);
        if (kind == "uuid" && concept.Primitive == SemanticPrimitiveType.Uuid)
        {
            throw new InvalidSemanticContract("An unmodified Uuid uuid generation must omit the generation member.");
        }
    }

    static void ValidateModifiers(SemanticConcept concept, SemanticGeneration generation)
    {
        var kind = generation.Kind;
        if (generation.Prefix is { } prefix && (kind == "typeid" ? !TypeIdPrefixRegex().IsMatch(prefix) : prefix.Length > 32 || !Printable(prefix)))
        {
            throw new InvalidSemanticContract("Generation prefix is invalid.");
        }

        if ((generation.Prefix is not null && concept.Primitive != SemanticPrimitiveType.Text) ||
            (generation.Case is not null && (!OneOf(generation.Case, "lower", "upper") ||
                !(kind == "ulid" || (concept.Primitive == SemanticPrimitiveType.Text && OneOf(kind, "uuid", "uuidv7")) || (kind == "bytes" && OneOf(generation.Encoding, "hex", "base32"))))) ||
            (kind != "nanoid" && (generation.Size is not null || generation.Alphabet is not null)) ||
            (kind != "bytes" && (generation.Count is not null || generation.Encoding is not null)))
        {
            throw new InvalidSemanticContract("Generation modifiers do not match their strategy or primitive.");
        }

        if (kind == "typeid" && generation.Prefix is null) throw new InvalidSemanticContract("TypeID requires its prefix argument (which may be empty).");
        if (kind == "nanoid" && (generation.Size is not (>= 1 and <= 256) ||
            (generation.Alphabet is { } alphabet && (alphabet == DefaultAlphabet || alphabet.Length is < 2 or > 256 || !Printable(alphabet) || alphabet.Distinct().Count() != alphabet.Length))))
        {
            throw new InvalidSemanticContract("NanoID requires a size of 1..256 and a non-default distinct printable ASCII alphabet when supplied.");
        }

        if (kind == "bytes" && (generation.Count is not (>= 1 and <= 512) || !OneOf(generation.Encoding, "base64url", "hex", "base32")))
        {
            throw new InvalidSemanticContract("Bytes requires a count of 1..512 and base64url, hex or base32 encoding.");
        }
    }

    static bool OneOf(string? value, params string[] choices) => choices.Contains(value, StringComparer.Ordinal);

    static bool Printable(string value) => value.All(character => character is >= ' ' and <= '~');

    [GeneratedRegex(@"^([a-z]([a-z_]{0,61}[a-z])?)?\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TypeIdPrefixRegex();
}
