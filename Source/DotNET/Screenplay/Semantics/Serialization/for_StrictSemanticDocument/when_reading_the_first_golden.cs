// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_StrictSemanticDocument;

public class when_reading_the_first_golden : Specification
{
    StrictSemanticDocument _document;
    SemanticRevision _expectedRevision;

    void Establish()
    {
        using var json = JsonDocument.Parse(canonical_serialization_golden_vectors.SemanticModelBytes);
        _expectedRevision = SemanticRevision.Parse(json.RootElement.GetProperty("revision").GetString()!);
    }

    void Because() => _document = StrictSemanticDocument.Read(canonical_serialization_golden_vectors.SemanticModelBytes);

    [Fact] void should_keep_the_original_bytes() => _document.CopyBytes().ShouldEqual(canonical_serialization_golden_vectors.SemanticModelBytes);
    [Fact] void should_keep_the_document_revision() => _document.Revision.ShouldEqual(_expectedRevision);
    [Fact] void should_keep_the_language_version() => _document.LanguageVersion.ShouldEqual(LanguageVersion.V1);
    [Fact] void should_keep_the_semantic_version() => _document.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_expose_the_validated_projection() => _document.Model.Revision.ShouldEqual(_document.Revision);
}
