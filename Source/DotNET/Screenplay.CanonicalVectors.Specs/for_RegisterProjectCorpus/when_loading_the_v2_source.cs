// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.for_RegisterProjectCorpus;

public class when_loading_the_v2_source : Specification
{
    CanonicalCorpusVector _corpus = null!;
    SemanticCompilation _compilation = null!;
    SemanticSpecificationRun[] _runs = null!;

    void Because()
    {
        _corpus = RegisterProjectCorpus.V2;
        var form = _corpus.SourceForms.Single();
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile(
            _corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
        _compilation = result.Value!;
        var plan = SemanticExecutionPlan.Compile(_compilation.Model).Plan!;
        _runs = [.. _corpus.SpecificationExpectations.Select(expectation => new SemanticSpecificationRunner().Run(plan, expectation.Specification))];
    }

    [Fact] void should_keep_the_v2_corpus_name() => _corpus.Name.ShouldEqual("register-project/v2");
    [Fact] void should_keep_the_application_identity() => _corpus.ApplicationIdentity.ToString().ShouldEqual("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994");
    [Fact] void should_keep_the_runtime_stream_identity() => _corpus.RuntimeStreamId.ShouldEqual("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    [Fact] void should_bind_v2() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V2);
    [Fact] void should_keep_the_application_semantic_identity() => _compilation.Model.Application.Id.ToString().ShouldEqual("sem1:47c05a36fc575882b4b91adc96b6adcaa28aa102c6732d12e459f1bd2b30ee07");
    [Fact] void should_keep_the_event_contract_identity() => _compilation.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single().ContractId.ToString().ShouldEqual("evt1:46abf8a1c198cd2ce27642ee744764d0797b844f7bc66fbd3f8c311fbea72f62");
    [Fact] void should_keep_both_specification_identities() => _corpus.SpecificationExpectations.Select(expectation => expectation.Specification.ToString()).ShouldEqual("sem1:951a1e8506741ec7552de6a3cd3ef5814c0b3c8d9600ab00fd1c3a3255664ae0", "sem1:a65de3ac412b245dc9649944e9940214ee53f8e9b354941ea33a0f4bca62cf62");
    [Fact] void should_match_esm() => SemanticModelSerializer.Serialize(_compilation.Model).SequenceEqual(_corpus.EsmBytes).ShouldBeTrue();
    [Fact] void should_round_trip_esm_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_corpus.EsmBytes.AsSpan())).SequenceEqual(_corpus.EsmBytes).ShouldBeTrue();
    [Fact] void should_keep_the_literal_revision() => _corpus.SemanticRevision.ToString().ShouldEqual("rev1:53baac263c39c8e03e8318b09ac29e0882b2f0809ac538ecf72f9867b0877473");
    [Fact] void should_match_revision() => _compilation.Model.Revision.ShouldEqual(_corpus.SemanticRevision);
    [Fact] void should_pass_both_specifications() => _runs.All(run => run.Passed && run.Failures.IsEmpty).ShouldBeTrue();
    [Fact] void should_match_outcomes() => _runs.Select(run => run.Execution.Kind).ShouldEqual(_corpus.SpecificationExpectations.Select(expectation => expectation.Outcome));
}
