// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer.when_writing_value_variants;

public class and_the_kind_is_unknown : given.a_semantic_value_vector
{
    void Establish() => _value = new given.an_unsupported_semantic_value { Kind = (SemanticValueKind)999 };

    void Because() => _exception = Catch.Exception(() => SemanticModelCanonicalJson.SerializeExpressionVector([SemanticExpression.FromValue(_value)]));

    [Fact] void should_refuse_the_value() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_the_kind_before_the_unsupported_subtype() => _exception.Message.ShouldEqual("Unknown SemanticValueKind value '999'.");
}
