// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_loading_v6_source : Specification
{
    readonly CanonicalCorpusVector _vector = ReactionsCorpus.V6;

    [Fact] void should_pin_the_source_backed_revision() => _vector.SemanticRevision.ToString().ShouldEqual("rev1:9f75777d7f5a031d3da39b1f9fce56b3c9c10e19fc4b4ba77d5b2857b46357d7");
    [Fact] void should_expect_every_specification() => _vector.SpecificationExpectations.Select(expectation => expectation.Name).ShouldEqual("SendingAnInvoice", "ClosingAPaidInvoice", "IssuingTheWeeklyDigest", "SeeingALegacyPayment");
    [Fact] void should_admit_the_version_and_run_every_specification_in_every_source_form()
    {
        _vector.SourceForms.Select(form => form.Name).ShouldEqual("single", "folder", "reordered", "relocated");
        foreach (var form in _vector.SourceForms)
        {
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
                catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
            var result = new SemanticModelCompiler().Compile(_vector.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
            result.Success.ShouldBeTrue();
            result.Diagnostics.ShouldBeEmpty();
            var model = result.Value!.Model;
            model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
            model.Revision.ShouldEqual(_vector.SemanticRevision);
            SemanticModelSerializer.Serialize(model).SequenceEqual(_vector.EsmBytes).ShouldBeTrue();
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            foreach (var expectation in _vector.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Passed.ShouldBeTrue();
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
            }
        }
    }
    [Fact] void should_round_trip_canonical_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_vector.EsmBytes.AsSpan())).SequenceEqual(_vector.EsmBytes).ShouldBeTrue();
}
