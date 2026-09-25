// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_event_generation_before_lineage_admission : given.a_semantic_binder
{
    const string Source =
        "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n" +
        "      event ProjectRegistered generation 1\n        projectId Uuid\n" +
        "      event ProjectRegistered generation 2\n        name String\n";

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_one_aggregate() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Events.Length.ShouldEqual(1);
    [Fact] void should_select_v4() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V4);
    [Fact] void should_bootstrap_every_revision_in_the_catalog()
    {
        var catalog = _result.Value!.Documents.IdentityCatalog;
        catalog.EventContracts.Single().Revision.Value.ShouldEqual(2u);
        var addresses = catalog.Semantics.Where(value => value.Address.Kind == SemanticKind.Property).Select(value => value.Address).ToArray();
        addresses.ShouldContain(address => address.Parts.Any(part => part.Kind == SemanticAddressPartKind.Generation && part.Key == "1"));
        addresses.ShouldContain(address => address.Parts.Any(part => part.Kind == SemanticAddressPartKind.Generation && part.Key == "2"));
    }
    [Fact] void should_link_predecessors()
    {
        var @event = _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single();
        @event.Revision.Value.ShouldEqual(2u);
        @event.Predecessor!.Value.Value.ShouldEqual(1u);
        @event.PriorRevisions.Single().Predecessor.ShouldBeNull();
        @event.PriorRevisions.Single().Properties.Single().Name.ShouldEqual("projectId");
        @event.Properties.Single().Name.ShouldEqual("name");
        @event.FindRevision(@event.ContractId, new(1))!.Properties.Single().Name.ShouldEqual("projectId");
        @event.FindRevision(@event.ContractId, new(2))!.Properties.Single().Name.ShouldEqual("name");
    }

    [Fact] void should_not_activate_v4_for_a_lone_generation_one()
    {
        var result = Bind("module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered generation 1\n        name String\n");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    }

    [Fact] void should_accept_an_unmarked_first_generation()
    {
        var result = Bind("module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered\n        projectId Uuid\n      event ProjectRegistered generation 2\n        name String\n");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V4);
    }

    [Fact] void should_reject_a_specification_against_the_old_shape()
    {
        var result = Bind(Source + "      specification ExpectOriginal\n        given ProjectRegistered\n          projectId = \"00000000-0000-0000-0000-000000000123\"\n");
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single(value => value.Code == DiagnosticCodes.UnsupportedEventGenerationSemantics);
        diagnostic.Message.ShouldContain("ProjectRegistered");
        diagnostic.Message.ShouldContain("revision 1");
    }

    [Fact] void should_refuse_fewer_generations_than_the_persisted_catalog()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered generation 1\n        name String\n";
        var address = SemanticAddress.ForEventContract(
            SemanticAddress.ForSlice(_applicationIdentity, "Projects", "Registration", "RegisterProject"), "ProjectRegistered");
        var catalog = SemanticIdentityCatalog.Create(
            _applicationIdentity,
            [],
            [],
            [new(address, EventContractId.CreateLegacy(_applicationIdentity, address.Name), new(2), SemanticIdentityOrigin.Persisted)]);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application-document"), "application-document", "application.play", source);
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var result = _binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single(value => value.Code == DiagnosticCodes.UnsupportedEventGenerationSemantics)
            .Message.ShouldContain("fewer than persisted catalog revision 2");
    }

    [Fact] void should_reject_a_mapping_to_a_historical_property()
    {
        var result = Bind(Source + "      command RegisterProject\n        projectId Uuid identifier\n        produces ProjectRegistered\n          projectId = projectId\n");
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single(value => value.Code == DiagnosticCodes.UnsupportedEventGenerationSemantics);
        diagnostic.Message.ShouldContain("ProjectRegistered");
        diagnostic.Message.ShouldContain("revision 1");
    }
}
