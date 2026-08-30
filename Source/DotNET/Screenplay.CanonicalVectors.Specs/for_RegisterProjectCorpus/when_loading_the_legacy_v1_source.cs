// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.for_RegisterProjectCorpus;

public class when_loading_the_legacy_v1_source : Specification
{
    CanonicalCorpusVector _corpus = null!;
    SemanticCompilation[] _results = null!;
    SemanticSpecificationRun[] _runs = null!;

    void Because()
    {
        _corpus = RegisterProjectCorpus.LegacyV1;
        _results =
        [
            .. _corpus.SourceForms.Select(form =>
            {
                var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
                var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
                    catalog.ResolveDocument(document.StableKey),
                    document.StableKey,
                    document.DisplayPath,
                    document.Text));
                var result = new SemanticModelCompiler().Compile(
                    _corpus.ApplicationName,
                    SemanticDocumentSet.Create([.. documents], catalog));
                result.Success.ShouldBeTrue();
                result.Diagnostics.ShouldBeEmpty();
                return result.Value!;
            })
        ];
        var plan = SemanticExecutionPlan.Compile(_results[0].Model).Plan!;
        var runner = new SemanticSpecificationRunner();
        _runs = [.. _corpus.SpecificationExpectations.Select(expectation => runner.Run(plan, expectation.Specification))];
    }

    [Fact] void should_keep_the_corpus_identity() => _corpus.Name.ShouldEqual("register-project/v1-legacy");
    [Fact] void should_keep_the_fixed_application_identity() => _corpus.ApplicationIdentity.ToString().ShouldEqual("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994");
    [Fact] void should_keep_the_fixed_runtime_stream_identity() => _corpus.RuntimeStreamId.ShouldEqual("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    [Fact] void should_expose_single_and_folder_source_forms() => _corpus.SourceForms.Select(form => form.Name).ShouldEqual("single", "folder");
    [Fact] void should_keep_the_single_stable_document_key() => _corpus.SourceForms[0].Documents.Single().StableKey.ShouldEqual("register-project-vector");
    [Fact] void should_keep_the_single_portable_display_path() => _corpus.SourceForms[0].Documents.Single().DisplayPath.ShouldEqual("RegisterProject.play");
    [Fact] void should_bind_the_expected_application_name() => _results.All(result => result.Model.Application.Name == "Projects").ShouldBeTrue();
    [Fact] void should_keep_the_exact_application_semantic_identity() => _results.All(result => result.Model.Application.Id.ToString() == "sem1:47c05a36fc575882b4b91adc96b6adcaa28aa102c6732d12e459f1bd2b30ee07").ShouldBeTrue();
    [Fact] void should_keep_the_exact_event_contract_identity() => _results.All(result => result.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single().ContractId.ToString() == "evt1:46abf8a1c198cd2ce27642ee744764d0797b844f7bc66fbd3f8c311fbea72f62").ShouldBeTrue();
    [Fact] void should_keep_the_exact_semantic_revision() => _corpus.SemanticRevision.ToString().ShouldEqual("rev1:ebe0c17eebc8aa64c573afe1ee639298e2874dd33406b8a9a663dd70aad820e4");
    [Fact] void should_pin_both_specification_ids() => _corpus.SpecificationExpectations.Select(expectation => expectation.Specification.ToString()).ShouldEqual("sem1:951a1e8506741ec7552de6a3cd3ef5814c0b3c8d9600ab00fd1c3a3255664ae0", "sem1:a65de3ac412b245dc9649944e9940214ee53f8e9b354941ea33a0f4bca62cf62");
    [Fact] void should_pin_rejected_then_accepted_outcomes() => _corpus.SpecificationExpectations.Select(expectation => expectation.Outcome.ToString()).ShouldEqual("Rejected", "Accepted");
    [Fact] void should_execute_every_pinned_specification_successfully() => _runs.All(run => run.Passed && run.Failures.IsEmpty).ShouldBeTrue();
    [Fact] void should_match_every_pinned_outcome() => _runs.Select(run => run.Execution.Kind).ShouldEqual(_corpus.SpecificationExpectations.Select(expectation => expectation.Outcome));
    [Fact] void should_make_all_source_forms_semantically_identical() => _results.All(result => result.Model.Revision == _corpus.SemanticRevision).ShouldBeTrue();
    [Fact] void should_match_the_checked_in_canonical_esm() => _results.All(result => SemanticModelSerializer.Serialize(result.Model).SequenceEqual(_corpus.EsmBytes)).ShouldBeTrue();
    [Fact] void should_keep_distinct_document_catalogs_for_distinct_physical_forms() => _corpus.SourceForms[0].IdentityCatalogBytes.SequenceEqual(_corpus.SourceForms[1].IdentityCatalogBytes).ShouldBeFalse();
}
