// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer.when_writing_value_variants;

public class and_the_subtype_is_unsupported : given.a_semantic_value_vector
{
    void Establish() => _value = new given.an_unsupported_semantic_value();

    void Because() => _exception = Catch.Exception(() => SemanticModelCanonicalJson.SerializeExpressionVector([SemanticExpression.FromValue(_value)]));

    [Fact] void should_refuse_the_value() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_explain_the_unsupported_variant() => _exception.Message.ShouldEqual("A semantic value variant is malformed or unknown.");
}
