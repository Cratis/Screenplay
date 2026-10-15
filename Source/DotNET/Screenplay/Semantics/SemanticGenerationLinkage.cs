// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static class SemanticGenerationLinkage
{
    internal static void Validate(SemanticApplication application, ImmutableArray<SemanticImplementationRequirement> requirements)
    {
        var generators = application.Concepts.Where(concept => concept.Generation?.Kind == "implementation").ToArray();
        foreach (var concept in generators)
        {
            var matches = requirements.Where(requirement => requirement.RequirementId == concept.Generation!.RequirementId).ToArray();
            if (matches.Length != 1) throw new InvalidSemanticContract("Implementation generation requires exactly one matching ValueGeneration attachment.");
            ValidateRequirement(concept, matches[0]);
        }

        if (requirements.Any(requirement => requirement.Role == SemanticImplementationRole.ValueGeneration &&
            !generators.Any(concept => concept.Generation!.RequirementId == requirement.RequirementId)))
        {
            throw new InvalidSemanticContract("A ValueGeneration attachment has no matching generation declaration.");
        }
    }

    static void ValidateRequirement(SemanticConcept concept, SemanticImplementationRequirement requirement)
    {
        if (requirement.Role != SemanticImplementationRole.ValueGeneration || requirement.Source.SemanticId != concept.Id ||
            requirement.Owner.Kind != SemanticKind.Concept || requirement.Owner.Name != concept.Name || string.IsNullOrEmpty(requirement.Member) ||
            requirement.RequirementId != ImplementationRequirementIdentity.Create(concept.Id, requirement.Role, requirement.Member) ||
            requirement.ContextVersion != 1 || requirement.ResultVersion != 1 || requirement.RequiredCapability != "provider-defined")
        {
            throw new InvalidSemanticContract("ValueGeneration role, owner, identity, capability or context/result version does not match generation.");
        }
        if ((requirement.File is null) == (requirement.Language is null)) throw new InvalidSemanticContract("ValueGeneration requires exactly one file or tagged inline body source.");
        if (requirement.AttachmentResolution == SemanticAttachmentResolution.Resolved)
        {
            if (requirement.BodySpan is null || requirement.ContentHash.Length != 64 || requirement.ContentHash.Any(character => !char.IsAsciiHexDigitLower(character)))
            {
                throw new InvalidSemanticContract("A resolved ValueGeneration body requires its content hash and source span.");
            }
        }
        else if (requirement.AttachmentResolution != SemanticAttachmentResolution.UnresolvedFile || requirement.File is null || requirement.ContentHash.Length != 0 || requirement.BodySpan is not null)
        {
            throw new InvalidSemanticContract("An unresolved ValueGeneration file cannot claim a resolved body.");
        }
    }
}
