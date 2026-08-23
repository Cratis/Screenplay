// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_all_expression_discriminators : Specification
{
    byte[] _expected;
    byte[] _serialized;

    void Establish() => _expected = canonical_serialization_golden_vectors.ExpressionBytes;

    void Because() => _serialized = SemanticModelCanonicalJson.SerializeExpressionVector(canonical_serialization_golden_vectors.CreateExpressions());

    [Fact] void should_match_the_checked_in_utf8_bytes() => _serialized.SequenceEqual(_expected).ShouldBeTrue();
}
#endif
