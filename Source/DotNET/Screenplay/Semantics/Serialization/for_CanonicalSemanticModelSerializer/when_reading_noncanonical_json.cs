// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_noncanonical_json : a_valid_semantic_model
{
    Exception _exception;

    void Because()
    {
        var json = SemanticModelSerializer.Serialize(_model);
        var withWhitespace = new byte[json.Length + 1];
        withWhitespace[0] = (byte)' ';
        json.CopyTo(withWhitespace, 1);
        _exception = Catch.Exception(() => SemanticModelSerializer.Deserialize(withWhitespace));
    }

    [Fact] void should_reject_the_document() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
