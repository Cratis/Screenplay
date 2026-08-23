// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_a_duplicate_field : a_valid_semantic_model
{
    Exception _exception;

    void Because()
    {
        var json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model));
        var duplicate = json.Replace(
            "\"schema\":\"cratis.screenplay.esm\"",
            "\"schema\":\"cratis.screenplay.esm\",\"schema\":\"cratis.screenplay.esm\"",
            StringComparison.Ordinal);
        _exception = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(duplicate)));
    }

    [Fact] void should_reject_the_document() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
