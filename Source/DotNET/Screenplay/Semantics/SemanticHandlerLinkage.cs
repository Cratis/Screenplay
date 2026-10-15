// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static class SemanticHandlerLinkage
{
    internal static void Validate(SemanticApplication application, ImmutableArray<SemanticImplementationRequirement> requirements)
    {
        var commands = application.Modules.SelectMany(AllSlices).SelectMany(slice => slice.Commands).Where(command => command.Handler is not null).ToArray();
        foreach (var command in commands)
        {
            var matches = requirements.Where(requirement => requirement.RequirementId == command.Handler!.RequirementId).ToArray();
            if (matches.Length != 1) throw new InvalidSemanticContract("A described command requires exactly one matching pending CommandHandler requirement.");
            ValidateRequirement(command, matches[0]);
            var index = SemanticCompilationIndex.Create(application, matches[0].Owner.Application);
            if (!index.Declarations.TryGetValue(matches[0].Owner, out var owner) || owner != command.Id)
            {
                throw new InvalidSemanticContract("CommandHandler owner address does not match the described command.");
            }
        }

        if (requirements.Any(requirement => requirement.Role == SemanticImplementationRole.CommandHandler && requirement.AttachmentResolution == SemanticAttachmentResolution.Pending &&
            !commands.Any(command => command.Handler!.RequirementId == requirement.RequirementId)))
        {
            throw new InvalidSemanticContract("A pending CommandHandler requirement has no matching described command.");
        }
    }

    static void ValidateRequirement(SemanticCommand command, SemanticImplementationRequirement requirement)
    {
        if (requirement.Role != SemanticImplementationRole.CommandHandler || requirement.Source.SemanticId != command.Id ||
            requirement.Owner.Kind != SemanticKind.Command || requirement.Owner.Name != command.Name || requirement.Member is not null ||
            requirement.RequirementId != ImplementationRequirementIdentity.Create(command.Id, SemanticImplementationRole.CommandHandler, null) ||
            requirement.ContextVersion != 1 || requirement.ResultVersion != 1 || command.Handler!.ResultVersion != 1 || requirement.RequiredCapability != "provider-defined")
        {
            throw new InvalidSemanticContract("CommandHandler role, owner, identity, capability or context/result version does not match the described command.");
        }

        if (requirement.AttachmentResolution != SemanticAttachmentResolution.Pending || requirement.File is not null || requirement.Language is not null ||
            requirement.ContentHash.Length != 0 || requirement.BodySpan is not null || !requirement.BodyLines.IsEmpty || requirement.Hints.IsDefault)
        {
            throw new InvalidSemanticContract("A described CommandHandler requirement must be pending without an attached body.");
        }
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticModule module) => module.Features.SelectMany(AllSlices);

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}
