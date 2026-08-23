// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_relocating_a_source_document : Specification
{
    SemanticDocumentSet _first;
    SemanticDocumentSet _relocated;

    void Because()
    {
        var id = DocumentId.Create("projects-main");
        _first = SemanticDocumentSet.Create([SemanticSourceDocument.Create(id, "projects-main", "old/projects.play", "module Projects")], SemanticIdentityCatalog.Empty);
        _relocated = SemanticDocumentSet.Create([SemanticSourceDocument.Create(id, "projects-main", "new/split/projects.play", "module Projects")], SemanticIdentityCatalog.Empty);
    }

    [Fact] void should_preserve_document_identity() => _relocated.Documents.Single().Id.ShouldEqual(_first.Documents.Single().Id);
    [Fact] void should_keep_the_display_path_out_of_identity() => _relocated.Documents.Single().DisplayPath.ShouldNotEqual(_first.Documents.Single().DisplayPath);
}
