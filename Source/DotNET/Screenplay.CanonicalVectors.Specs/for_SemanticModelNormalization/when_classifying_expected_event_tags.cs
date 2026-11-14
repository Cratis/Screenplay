// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Conformance;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelNormalization;

public class when_classifying_expected_event_tags : Specification
{
    [Theory]
    [InlineData("[]")]
    [InlineData("[\"audit\",\"billing\",\"audit\"]")]
    [InlineData("[\"sem1:0000000000000000000000000000000000000000000000000000000000000000\"]")]
    void should_treat_expected_tags_as_literals(string tags)
    {
        using var document = JsonDocument.Parse($$"""{"expectedTags":{{tags}}}""");
        SemanticStringClassification.Classify(document.RootElement, "thenEvents", "expectedTags").ShouldEqual(SemanticStringKind.Literal);
    }

    [Fact]
    void should_preserve_the_canonical_writers_order() =>
        SemanticArrayClassification.Require("thenEvents", "expectedTags", "/thenEvents/expectedTags").Kind.ShouldEqual(SemanticArrayKind.Ordered);
}
