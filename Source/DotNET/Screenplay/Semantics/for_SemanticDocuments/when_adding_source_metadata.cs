// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_adding_source_metadata : a_valid_semantic_model
{
    SemanticRevision _before;
    SemanticRevision _after;

    void Because()
    {
        _before = _model.Revision;
        var documentId = DocumentId.Create("projects-main");
        var document = SemanticSourceDocument.Create(documentId, "projects-main", "projects.play", "module Projects");
        _ = SemanticSourceMap.Create(
            [new(_applicationId, SemanticSourceSpan.Create(documentId, 0, 7, 1, 1, 1, 8), SemanticIdentityOrigin.Persisted)],
            [document]);
        _after = _model.Revision;
    }

    [Fact] void should_keep_source_maps_out_of_semantic_revision() => _after.ShouldEqual(_before);
}
