// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesConceptCompliance(SemanticApplication application) => application.Concepts.Any(concept => concept.Compliance is not null);

    static void ValidateComplianceVersion(SemanticApplication application, SemanticVersion version)
    {
        var uses = UsesConceptCompliance(application);
        if (uses && !version.IsAtLeast(SemanticVersion.V13)) throw new InvalidSemanticContract("Concept compliance requires ESM v13 or later.");
        if (!uses && version == SemanticVersion.V13) throw new InvalidSemanticContract("An ESM v13 model must use concept compliance.");
    }

    static void ValidateCompliance(SemanticConcept concept)
    {
        if (concept.Compliance is not { } compliance) return;
        if (compliance.Personal is null && compliance.Secret is null) throw new InvalidSemanticContract($"Concept '{concept.Name}' compliance requires a personal or secret marker.");
        if (!concept.Values.IsDefaultOrEmpty) throw new InvalidSemanticContract($"Enum concept '{concept.Name}' cannot carry compliance.");
        if (compliance.Personal?.Special is { } special && !ConceptComplianceParser.SpecialCategories.Contains(special, StringComparer.Ordinal))
        {
            throw new InvalidSemanticContract($"Concept '{concept.Name}' has an unknown personal special category '{special}'.");
        }
        if (compliance.Secret?.Scope is { } scope)
        {
            if (compliance.Personal is not null) throw new InvalidSemanticContract($"Concept '{concept.Name}' cannot store secret scope next to personal classification.");
            if (scope is not ("subject" or "namespace" or "global")) throw new InvalidSemanticContract($"Concept '{concept.Name}' has an unknown secret scope '{scope}'.");
        }
    }

    private sealed partial class ValidationContext
    {
        void ValidateComplianceIdentity(SemanticTypeReference? type)
        {
            if (type is { Kind: SemanticTypeReferenceKind.Concept } && _concepts.TryGetValue(type.Target, out var concept) && concept.Compliance is not null)
            {
                throw new InvalidSemanticContract($"Concept '{concept.Name}' compliance cannot be used as an event source or stream identity; use a surrogate identity.");
            }
        }
    }
}
