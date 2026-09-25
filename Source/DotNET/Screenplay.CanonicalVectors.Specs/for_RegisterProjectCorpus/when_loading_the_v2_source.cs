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
        var form = _corpus.SourceForms[0];
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
    [Fact] void should_bind_v4_lineage() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V4);
    [Fact] void should_remove_the_current_payload_identity()
    {
        var @event = _compilation.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single();
        @event.Revision.Value.ShouldEqual(2u);
        @event.Properties.Select(property => property.Name).ShouldEqual("name");
        @event.PriorRevisions.Single().Properties.Select(property => property.Name).ShouldContain("projectId");
        @event.PriorRevisions.Single().Properties.Single(property => property.Name == "projectId").Id.ToString()
            .ShouldEqual("sem1:2d4a35834b92530e098d19ae2d444e08081c49facd67a62090635d61e6bfdb81");
        @event.PriorRevisions.Single().Properties.Single(property => property.Name == "name").Id.ToString()
            .ShouldEqual("sem1:ae4d70b46191262ea2320e51c459ba2a0550a4b2522208f0410dbc96e6c72e1e");
        @event.Properties.Single().Id.ToString()
            .ShouldEqual("sem1:e3a80a92489f4d7ac450898031cc554abc4515a601733721f6406ece55d932c2");
    }
    [Fact] void should_keep_the_application_semantic_identity() => _compilation.Model.Application.Id.ToString().ShouldEqual("sem1:47c05a36fc575882b4b91adc96b6adcaa28aa102c6732d12e459f1bd2b30ee07");
    [Fact] void should_keep_the_event_contract_identity() => _compilation.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single().ContractId.ToString().ShouldEqual("evt1:46abf8a1c198cd2ce27642ee744764d0797b844f7bc66fbd3f8c311fbea72f62");
    [Fact] void should_preserve_unchanged_command_read_model_and_query_identities()
    {
        var slices = _compilation.Model.Application.Modules.Single().Features.Single().Slices;
        slices.SelectMany(slice => slice.Commands).Single().Id.ToString()
            .ShouldEqual("sem1:f212d36ad0fa55630a5af74a74ed6424af1cb5af6da3652e616cc695292c8766");
        slices.SelectMany(slice => slice.ReadModels).Single().Id.ToString()
            .ShouldEqual("sem1:e2c72079e285bcffaaf87eedbda830171d27268dc030a3cb427bdbd11cabe9e1");
        slices.SelectMany(slice => slice.Queries).Single().Id.ToString()
            .ShouldEqual("sem1:a5f617c2c41e1fc5e3467488f00f5c5fbc9e778eb7327399851d58297161297e");
    }
    [Fact] void should_keep_both_specification_identities() => _corpus.SpecificationExpectations.Select(expectation => expectation.Specification.ToString()).ShouldEqual("sem1:951a1e8506741ec7552de6a3cd3ef5814c0b3c8d9600ab00fd1c3a3255664ae0", "sem1:a65de3ac412b245dc9649944e9940214ee53f8e9b354941ea33a0f4bca62cf62");
    [Fact] void should_match_esm() => SemanticModelSerializer.Serialize(_compilation.Model).SequenceEqual(_corpus.EsmBytes).ShouldBeTrue();
    [Fact] void should_match_all_folder_reordered_and_relocated_forms()
    {
        _corpus.SourceForms.Select(form => form.Name).ShouldEqual("single", "folder", "reordered", "relocated");
        foreach (var form in _corpus.SourceForms)
        {
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
                catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
            var result = new SemanticModelCompiler().Compile(_corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
            result.Success.ShouldBeTrue();
            result.Value!.Model.Revision.ShouldEqual(_corpus.SemanticRevision);
            SemanticModelSerializer.Serialize(result.Value.Model).SequenceEqual(_corpus.EsmBytes).ShouldBeTrue();
            var plan = SemanticExecutionPlan.Compile(result.Value.Model).Plan!;
            _corpus.SpecificationExpectations.Select(expectation => new SemanticSpecificationRunner().Run(plan, expectation.Specification).Execution.Kind)
                .ShouldEqual(_corpus.SpecificationExpectations.Select(expectation => expectation.Outcome));
        }
    }
    [Fact] void should_round_trip_esm_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_corpus.EsmBytes.AsSpan())).SequenceEqual(_corpus.EsmBytes).ShouldBeTrue();
    [Fact] void should_keep_the_literal_revision() => _corpus.SemanticRevision.ToString().ShouldEqual("rev1:c2bef8cf2ba598b564f67d9dc96a5555f4246ac33c6937cb83409b71acc2b3dd");
    [Fact] void should_match_revision() => _compilation.Model.Revision.ShouldEqual(_corpus.SemanticRevision);
    [Fact] void should_pass_both_specifications() => _runs.All(run => run.Passed && run.Failures.IsEmpty).ShouldBeTrue();
    [Fact] void should_match_outcomes() => _runs.Select(run => run.Execution.Kind).ShouldEqual(_corpus.SpecificationExpectations.Select(expectation => expectation.Outcome));
}
