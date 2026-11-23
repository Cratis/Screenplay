// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer.when_writing_array_values;

public class and_a_member_is_null : given.a_semantic_value_vector
{
    void Establish() => _value = SemanticValue.Array([null!]);

    void Because() => _exception = Catch.Exception(() => SemanticModelCanonicalJson.SerializeExpressionVector([SemanticExpression.FromValue(_value)]));

    [Fact] void should_refuse_the_value() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_explain_the_malformed_array() => _exception.Message.ShouldEqual("A semantic array value is malformed.");
}
