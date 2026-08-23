// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_using_colliding_portable_paths : Specification
{
    Exception[] _exceptions;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var catalog = SemanticIdentityCatalog.Empty(application);
        _exceptions =
        [
            Catch.Exception(() => SemanticDocumentSet.Create(
            [
                SemanticSourceDocument.Create(DocumentId.Create("first"), "first", "Folder/Source.play", string.Empty),
                SemanticSourceDocument.Create(DocumentId.Create("second"), "second", "folder/source.play", string.Empty)
            ],
            catalog)),
            Catch.Exception(() => SemanticDocumentSet.Create(
            [
                SemanticSourceDocument.Create(DocumentId.Create("first"), "first", "café.play", string.Empty),
                SemanticSourceDocument.Create(DocumentId.Create("second"), "second", "cafe\u0301.play", string.Empty)
            ],
            catalog))
        ];
    }

    [Fact] void should_reject_case_and_nfc_collisions_as_invalid_semantic_contracts() =>
        _exceptions.All(_ => _.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
}
#endif
