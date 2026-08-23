// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_SemanticCompilation.given;

public class a_valid_semantic_compilation : a_valid_semantic_model
{
    protected SemanticDocumentSet _documents;
    protected SemanticSourceDocument _document;
    protected SemanticSourceMap _sourceMap;

    void Establish()
    {
        var documentId = DocumentId.Create("projects-main");
        _document = SemanticSourceDocument.Create(documentId, "projects-main", "projects/main.play", "module Projects");
        _documents = SemanticDocumentSet.Create([_document], _catalog);
        _sourceMap = SemanticSourceMap.Create(
            [new(_applicationId, SemanticSourceSpan.Create(documentId, 0, 7, 1, 1, 1, 8), SemanticIdentityOrigin.Persisted)],
            [_document]);
    }
}
#endif
