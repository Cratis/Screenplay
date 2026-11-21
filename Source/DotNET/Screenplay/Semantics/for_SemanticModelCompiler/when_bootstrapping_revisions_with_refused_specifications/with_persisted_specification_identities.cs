// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelCompiler.when_bootstrapping_revisions_with_refused_specifications;

public class with_persisted_specification_identities : given.a_specification_refusal_compiler
{
    CompilationResult<SemanticCompilation> _result;
    SemanticCompilation _baseline;
    SemanticIdentityAssignment _persisted;

    void Because()
    {
        var initial = Compile(Source).Value!;
        var catalog = initial.Documents.IdentityCatalog;
        var address = SemanticAddress.ForSpecification(SemanticAddress.ForSlice(catalog.Application, "M", ["F"], "Recording"), "RecordingWorks");
        _persisted = new(address, Recording(initial).Specifications.Single().Id, SemanticIdentityOrigin.Persisted);
        var identities = SemanticIdentityCatalog.Create(catalog.Application, catalog.Documents, [_persisted], catalog.EventContracts);
        var evolved = Source.Replace("event Recorded\n        amount Int", "event Recorded generation 1\n        amount Int\n      event Recorded generation 2\n        amount Int", StringComparison.Ordinal);
        var healthy = Compile(evolved, identities: identities);
        Assert.True(healthy.Success, string.Join('\n', healthy.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _baseline = healthy.Value!;
        _result = Compile(evolved.Replace("when Record amount = 10", "when Record", StringComparison.Ordinal), identities: identities);
    }

    [Fact] void should_keep_the_existing_failure_contract() => _result.Success.ShouldBeFalse();
    [Fact] void should_leave_the_existing_value_absent() => _result.Value.ShouldBeNull();
    [Fact] void should_offer_the_safe_compilation() => _result.SpecificationRefusedValue.ShouldNotBeNull();
    [Fact] void should_keep_the_refused_specification_identity() => _result.SpecificationRefusedValue!.RefusedSpecifications.Single().Id.ShouldEqual(_persisted.Id);
    [Fact] void should_keep_the_persisted_assignment() => _result.SpecificationRefusedValue!.Documents.IdentityCatalog.Semantics.ShouldContain(_persisted);
    [Fact] void should_keep_the_advanced_event_revision() => Recording(_result.SpecificationRefusedValue!).Events.Single().Revision.ShouldEqual(new EventContractRevision(2));
    [Fact] void should_keep_the_healthy_slice() => Confirming(_result.SpecificationRefusedValue!).Id.ShouldEqual(Confirming(_baseline).Id);
    [Fact] void should_keep_the_healthy_specification() => Confirming(_result.SpecificationRefusedValue!).Specifications.Single().Id.ShouldEqual(Confirming(_baseline).Specifications.Single().Id);
    [Fact] void should_keep_source_maps_coherent() => _result.SpecificationRefusedValue!.SourceMap.Entries.Select(entry => entry.SemanticId).ShouldNotContain(_persisted.Id);
}
