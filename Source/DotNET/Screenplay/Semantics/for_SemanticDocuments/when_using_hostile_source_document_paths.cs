// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_using_hostile_source_document_paths : Specification
{
    Exception[] _exceptions;

    void Because()
    {
        var id = DocumentId.Create("source");
        var invalidPaths = new[]
        {
            string.Empty,
            "/absolute.play",
            "C:/absolute.play",
            "C:\\absolute.play",
            "\\\\server\\share.play",
            "../traversal.play",
            "folder/../traversal.play",
            "folder/./source.play",
            "folder//source.play",
            "folder/",
            "source\0.play"
        };
        _exceptions =
        [
            .. invalidPaths.Select(path => Catch.Exception(() => SemanticSourceDocument.Create(id, "source", path, string.Empty))),
            Catch.Exception(() => SemanticSourceDocument.Create(id, "folder/source", "source.play", string.Empty)),
            Catch.Exception(() => SemanticSourceDocument.Create(id, "C:", "source.play", string.Empty)),
            Catch.Exception(() => DocumentId.Create("folder/source.play"))
        ];
    }

    [Fact] void should_reject_every_hostile_path_as_an_invalid_semantic_contract() =>
        _exceptions.All(_ => _.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
}
#endif
