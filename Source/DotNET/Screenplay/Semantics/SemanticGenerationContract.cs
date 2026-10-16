// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticGenerationContract
{
    internal static string DefaultAlphabet => GenerationStrategyCatalog.OptionFor("nanoid", "alphabet").Default!;

    internal static double? Bits(SemanticGeneration? generation) =>
        GenerationEntropy.Bits(generation?.Kind, generation?.Size, generation?.Alphabet, generation?.Count);

    internal static void Validate(SemanticConcept concept, int? utcAssessmentYear = null)
    {
        if (concept.Generation is not { } generation) return;
        var kind = generation.Kind;
        var strategy = GenerationStrategyCatalog.FindEsm(kind);
        var primitive = concept.Primitive switch
        {
            SemanticPrimitiveType.Text => "String",
            SemanticPrimitiveType.Uuid => "Uuid",
            SemanticPrimitiveType.WholeNumber => "Int",
            _ => string.Empty
        };
        if (strategy is null || strategy.Status != GenerationStrategyStatus.Admitted || !concept.Values.IsEmpty || !strategy.Primitives.Contains(primitive, StringComparer.Ordinal))
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
        if (generation.Prefix is { } prefix && !GenerationStrategyCatalog.ValidText(GenerationStrategyCatalog.OptionFor(kind == "typeid" ? "typeid" : "uuid", "prefix"), prefix))
        {
            throw new InvalidSemanticContract("Generation prefix is invalid.");
        }

        if ((generation.Prefix is not null && concept.Primitive != SemanticPrimitiveType.Text) ||
            (generation.Case is not null && (!GenerationStrategyCatalog.OptionFor("uuid", "case").Choices.Contains(generation.Case, StringComparer.Ordinal) ||
                !GenerationStrategyCatalog.AllowsModifier(kind, concept.Primitive == SemanticPrimitiveType.Text ? "String" : "Uuid", generation.Encoding, "case"))) ||
            (kind != "nanoid" && (generation.Size is not null || generation.Alphabet is not null)) ||
            (kind != "bytes" && (generation.Count is not null || generation.Encoding is not null)))
        {
            throw new InvalidSemanticContract("Generation modifiers do not match their strategy or primitive.");
        }

        if (kind == "typeid" && generation.Prefix is null) throw new InvalidSemanticContract("TypeID requires its prefix argument (which may be empty).");
        if (kind == "nanoid" && ((generation.Size is not { } size || !GenerationStrategyCatalog.InRange(GenerationStrategyCatalog.OptionFor("nanoid", "size"), size)) ||
            (generation.Alphabet is { } alphabet && (alphabet == DefaultAlphabet || !GenerationStrategyCatalog.ValidText(GenerationStrategyCatalog.OptionFor("nanoid", "alphabet"), alphabet)))))
        {
            throw new InvalidSemanticContract("NanoID requires a size of 1..256 and a non-default distinct printable ASCII alphabet when supplied.");
        }

        if (kind == "bytes" && (generation.Count is not { } count || !GenerationStrategyCatalog.InRange(GenerationStrategyCatalog.OptionFor("bytes", "count"), count) || generation.Encoding is null || !GenerationStrategyCatalog.OptionFor("bytes", "encoding").Choices.Contains(generation.Encoding, StringComparer.Ordinal)))
        {
            throw new InvalidSemanticContract("Bytes requires a count of 1..512 and base64url, hex or base32 encoding.");
        }
    }

    static bool OneOf(string? value, params string[] choices) => choices.Contains(value, StringComparer.Ordinal);
}
