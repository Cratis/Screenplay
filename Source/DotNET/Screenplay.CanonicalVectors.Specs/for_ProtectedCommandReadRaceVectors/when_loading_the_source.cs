// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors.given;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors;

public class when_loading_the_source : Specification
{
    CanonicalCorpusVector _model;
    CompilationResult<SemanticCompilation> _compiled;

    void Because()
    {
        _model = ProtectedCommandReadRaceVectors.Model;
        var form = _model.SourceForms.Single();
        _compiled = RaceVectorExecution.Compile(form.Documents.Single().Text, form.IdentityCatalogBytes);
    }

    [Fact] void should_compile_the_source_without_errors_or_warnings() => _compiled.Diagnostics.Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning).ShouldBeEmpty();
    [Fact] void should_select_v25_with_protected_reads() => (_compiled.Value!.Model.SemanticVersion == SemanticVersion.V25 && _compiled.Value.Model.Application.CommandReadMode == SemanticCommandReadMode.Protected).ShouldBeTrue();
    [Fact] void should_pin_the_esm_bytes() => SemanticModelSerializer.Serialize(_compiled.Value!.Model).SequenceEqual(_model.EsmBytes).ShouldBeTrue();
    [Fact] void should_pin_the_revision() => _compiled.Value!.Model.Revision.ShouldEqual(_model.SemanticRevision);
    [Fact] void should_round_trip_the_published_esm() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_model.EsmBytes.AsSpan())).SequenceEqual(_model.EsmBytes).ShouldBeTrue();
    [Fact] void should_pin_the_identity_catalog() => SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalogSerializer.Deserialize(_model.SourceForms.Single().IdentityCatalogBytes.AsSpan())).SequenceEqual(_model.SourceForms.Single().IdentityCatalogBytes).ShouldBeTrue();
}
