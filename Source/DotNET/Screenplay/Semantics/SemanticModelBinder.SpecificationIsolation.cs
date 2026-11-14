// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    static SemanticDocumentSet BootstrapEventRevisions(
        SemanticDocumentSet documents,
        SemanticApplication application,
        ImmutableArray<RefusedSpecification> refusals)
    {
        // Only new event addresses bootstrap here; persisted revisions still require an explicit plan.
        var index = SemanticCompilationIndex.Create(application, documents.IdentityCatalog.Application);
        var missing = index.Events
            .Where(entry => entry.Value.Revision.Value > 1 &&
                !documents.IdentityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(entry.Key)))
            .Select(entry => new EventContractRevisionAdvancement(entry.Key, entry.Value.Revision))
            .ToImmutableArray();
        if (missing.IsEmpty)
        {
            return documents;
        }

        var refusedIds = refusals.SelectMany(refusal => refusal.DeclarationIds.Prepend(refusal.Id)).ToHashSet();
        var refusedAddresses = documents.IdentityCatalog.Semantics.Where(assignment => refusedIds.Contains(assignment.Id)).Select(assignment => assignment.Address);
        var catalog = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            documents.IdentityCatalog,
            documents.IdentityCatalog.Revision,
            [.. documents.Documents.Select(value => value.StableKey)],
            [.. index.Declarations.Keys.Concat(refusedAddresses).Distinct()],
            [.. index.Events.Keys],
            missing).Catalog;
        return SemanticDocumentSet.Create(documents.Documents, catalog, documents.AttachmentContents);
    }

    private sealed partial class BindingContext
    {
        static bool SpecifiesEventSource(SemanticSpecification specification) =>
            specification.When?.EventSource is not null || specification.WhenAppended?.EventSource is not null ||
            specification.GivenEvents.Any(value => value.EventSource is not null) ||
            specification.ThenEvents.Any(value => value.EventSource is not null);

        static bool SpecifiesAutomation(SemanticSpecification specification) =>
            specification.GivenClock is not null || specification.WhenClock is not null ||
            specification.WhenTrigger is not null || specification.WhenCapture is not null || !specification.GivenCaptures.IsDefaultOrEmpty;

        SemanticApplication WithRetainedSpecificationVersions(SemanticApplication application)
        {
            var specifications = application.Modules.SelectMany(module => module.Features).SelectMany(AllBoundSlices).SelectMany(slice => slice.Specifications).ToArray();
            UsesV2 |= specifications.Any(SpecifiesEventSource);
            UsesV5 = specifications.Any(specification => !specification.ThenAbsentReadModels.IsDefaultOrEmpty);
            UsesV6 |= specifications.Any(SpecifiesAutomation);
            UsesV7 |= specifications.Any(specification => specification.ThenReturns is not null || specification.When is { GeneratedValues.IsDefaultOrEmpty: false });
            UsesV30 = SemanticModelValidator.UsesGivenGenerated(application);
            return UsesV2 || UsesV5 || UsesV6 || UsesV7 ? PromoteV2Destinations(application) : application;
        }
    }
}
