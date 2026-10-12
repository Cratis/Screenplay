// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool HasNonPositionalEnumNumbers(IEnumerable<int> numbers, int count) =>
        !numbers.SequenceEqual(Enumerable.Range(0, count));

    internal static bool UsesEnumMemberNumbers(SemanticApplication application) =>
        application.Concepts.Any(concept => concept.Numbers is { IsDefault: false } numbers && HasNonPositionalEnumNumbers(numbers, concept.Values.Length));

    static void ValidateEnumMemberNumbersVersion(SemanticApplication application, SemanticVersion version)
    {
        if (application.Concepts.Any(concept => concept.Numbers is not null) && !version.IsAtLeast(SemanticVersion.V12))
        {
            throw new InvalidSemanticContract("Enum member numbers require ESM v12 or later.");
        }

        if (version == SemanticVersion.V12 && !UsesEnumMemberNumbers(application))
        {
            throw new InvalidSemanticContract("An ESM v12 model must use non-positional enum member numbers.");
        }
    }

    static void ValidateEnumMemberNumbers(SemanticConcept concept)
    {
        if (concept.Numbers is not { } numbers) return;
        if (concept.Primitive != SemanticPrimitiveType.Text || concept.Values.IsDefaultOrEmpty)
        {
            throw new InvalidSemanticContract($"Concept '{concept.Name}' numbers require enum values and the text primitive.");
        }

        if (numbers.IsDefault || numbers.Length != concept.Values.Length)
        {
            throw new InvalidSemanticContract($"Concept '{concept.Name}' numbers must match the enum values' cardinality.");
        }

        if (numbers.Distinct().Count() != numbers.Length)
        {
            throw new InvalidSemanticContract($"Concept '{concept.Name}' enum member numbers must be unique.");
        }

        if (!HasNonPositionalEnumNumbers(numbers, concept.Values.Length))
        {
            throw new InvalidSemanticContract($"Concept '{concept.Name}' positional enum member numbers must be absent in canonical ESM.");
        }
    }
}
