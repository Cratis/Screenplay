// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_advancing_existing_and_new_event_assignments : Specification
{
    SemanticIdentityCatalog _previous;
    SemanticIdentityCatalogMigrationPlan _plan;
    SemanticAddress _existing;
    SemanticAddress _untouched;
    SemanticAddress _added;

    void Establish()
    {
        var application = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject");
        _existing = SemanticAddress.ForEventContract(slice, "ProjectRegistered");
        _untouched = SemanticAddress.ForEventContract(slice, "ProjectRenamed");
        _added = SemanticAddress.ForEventContract(slice, "ProjectImported");
        _previous = SemanticIdentityCatalog.Create(
            application,
            [],
            [],
            [new(_existing, EventContractId.CreateLegacy(application, "ImportedIdentity"), new(1), SemanticIdentityOrigin.Persisted),
             new(_untouched, EventContractId.CreateLegacy(application, _untouched.Name), new(1), SemanticIdentityOrigin.LegacyBootstrap)]);
    }

    void Because() => _plan = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
        _previous,
        _previous.Revision,
        [],
        [_existing, _untouched, _added],
        [_existing, _untouched, _added],
        [new(_existing, new(2)), new(_added, new(3))]);

    [Fact] void should_preserve_the_existing_event_identity() => _plan.Catalog.ResolveEventContract(_existing).Id.ShouldEqual(_previous.ResolveEventContract(_existing).Id);
    [Fact] void should_mark_the_advanced_existing_assignment_as_persisted() => _plan.Catalog.ResolveEventContract(_existing).Origin.ShouldEqual(SemanticIdentityOrigin.Persisted);
    [Fact] void should_advance_the_existing_revision() => _plan.Catalog.ResolveEventContract(_existing).Revision.Value.ShouldEqual(2u);
    [Fact] void should_bootstrap_the_new_event_identity() => _plan.Catalog.ResolveEventContract(_added).Id.ShouldEqual(EventContractId.CreateLegacy(_previous.Application, _added.Name));
    [Fact] void should_keep_the_new_assignment_as_legacy_bootstrap() => _plan.Catalog.ResolveEventContract(_added).Origin.ShouldEqual(SemanticIdentityOrigin.LegacyBootstrap);
    [Fact] void should_use_the_requested_new_event_revision() => _plan.Catalog.ResolveEventContract(_added).Revision.Value.ShouldEqual(3u);
    [Fact] void should_keep_the_untouched_assignment() => _plan.Catalog.ResolveEventContract(_untouched).ShouldEqual(_previous.ResolveEventContract(_untouched));
    [Fact] void should_keep_the_plan_bound_to_the_original_revision() => _plan.BaseRevision.ShouldEqual(_previous.Revision);
}
