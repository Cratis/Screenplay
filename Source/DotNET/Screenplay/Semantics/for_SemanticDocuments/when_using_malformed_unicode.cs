// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_SemanticDocuments;

public class when_using_malformed_unicode : Specification
{
    Exception[] _exceptions;

    void Because()
    {
        var id = DocumentId.Create("source");
        _exceptions =
        [
            Catch.Exception(() => SemanticSourceDocument.Create(id, "source\ud800", "source.play", string.Empty)),
            Catch.Exception(() => SemanticSourceDocument.Create(id, "source", "source\udfff.play", string.Empty)),
            Catch.Exception(() => SemanticSourceDocument.Create(id, "source", "source.play", "module \ud800"))
        ];
    }

    [Fact] void should_reject_every_malformed_value_as_an_invalid_semantic_contract() =>
        _exceptions.All(_ => _.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
}
#endif
