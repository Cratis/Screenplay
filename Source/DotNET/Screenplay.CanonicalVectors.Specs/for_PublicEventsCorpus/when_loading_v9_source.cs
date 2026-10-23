// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PublicEventsCorpus;

public class when_loading_v9_source : Specification
{
    readonly CanonicalCorpusVector _vector = PublicEventsCorpus.V9;

    [Fact] void should_pin_the_source_backed_revision() => _vector.SemanticRevision.ToString().ShouldEqual("rev1:8ffa4d7ac03bf38338c7aedb16187229c6ab8139ecfdb0039524e801ea956c2f");
    [Fact] void should_rebind_the_typed_capture_source_to_v32_without_rewriting_the_v9_vector()
    {
        var form = _vector.SourceForms.Single();
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile(_vector.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        result.Success.ShouldBeTrue();
        var model = result.Value!.Model;
        model.SemanticVersion.ShouldEqual(SemanticVersion.V32);
        (model.Revision == _vector.SemanticRevision).ShouldBeFalse();
        var type = SemanticTypeReference.ForConcept(model.Application.Concepts.Single(concept => concept.Name == "OrderId").Id);
        model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "TrackShipments")
            .Captures.Single().Appends.Single().EventSourceType.ShouldEqual(type);
        SemanticModelSerializer.Serialize(model).SequenceEqual(_vector.EsmBytes).ShouldBeFalse();
    }

    [Fact] void should_run_the_publication_specification()
    {
        var form = _vector.SourceForms.Single();
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var model = new SemanticModelCompiler().Compile(_vector.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog)).Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var run = new SemanticSpecificationRunner().Run(plan, _vector.SpecificationExpectations.Single().Specification);
        run.Passed.ShouldBeTrue();
        run.Execution.Kind.ShouldEqual(_vector.SpecificationExpectations.Single().Outcome);
    }

    [Fact] void should_round_trip_canonical_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_vector.EsmBytes.AsSpan())).SequenceEqual(_vector.EsmBytes).ShouldBeTrue();
}
