// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReadModelAbsenceCorpus;

public class when_loading_v5_source : Specification
{
    readonly CanonicalCorpusVector _vector = ReadModelAbsenceCorpus.V5;

    [Fact] void should_pin_the_source_backed_revision() => _vector.SemanticRevision.ToString().ShouldEqual("rev1:f9497caeba58c9bc0aecb5f4867d31c8eefc5a197172e0a168282717d221956d");
    [Fact] void should_admit_the_version_in_every_source_form()
    {
        _vector.SourceForms.Select(form => form.Name).ShouldEqual("single", "folder", "reordered", "relocated");
        foreach (var form in _vector.SourceForms)
        {
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
                catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
            var result = new SemanticModelCompiler().Compile(_vector.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
            result.Success.ShouldBeTrue();
            result.Diagnostics.ShouldEachConformTo(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix && diagnostic.Severity == DiagnosticSeverity.Information);
            var model = result.Value!.Model;
            model.SemanticVersion.ShouldEqual(SemanticVersion.V5);
            model.Revision.ShouldEqual(_vector.SemanticRevision);
            SemanticModelSerializer.Serialize(model).SequenceEqual(_vector.EsmBytes).ShouldBeTrue();
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            var run = new SemanticSpecificationRunner().Run(plan, _vector.SpecificationExpectations.Single().Specification);
            run.Passed.ShouldBeTrue();
            run.Execution.Kind.ShouldEqual(_vector.SpecificationExpectations.Single().Outcome);
            run.Execution.World.ReadModels.Single().Key.ShouldEqual(SemanticValue.Text("second"));
        }
    }
    [Fact] void should_round_trip_canonical_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_vector.EsmBytes.AsSpan())).SequenceEqual(_vector.EsmBytes).ShouldBeTrue();
}
