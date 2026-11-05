// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Conformance;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelNormalization;

public class when_classifying_possible_event_arrays : Specification
{
    [Theory]
    [InlineData("handler", "possibleEvents")]
    [InlineData("possibleEvents", "mappings")]
    [InlineData("possibleEvents", "tags")]
    void should_preserve_the_canonical_writers_order(string owner, string property) =>
        SemanticArrayClassification.Require(owner, property, $"/{owner}/{property}").Kind.ShouldEqual(SemanticArrayKind.Ordered);
}
