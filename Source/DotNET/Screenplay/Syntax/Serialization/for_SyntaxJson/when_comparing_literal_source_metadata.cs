// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_comparing_literal_source_metadata : Specification
{
    LiteralExpressionSyntax _original;
    LiteralExpressionSyntax _relocated;
    bool _equal;

    void Establish()
    {
        _original = new LiteralExpressionSyntax("web", SourceLocation.Start);
        _relocated = _original with { RawLocation = new SourceLocation(4, 21, "other.play"), RawLength = 5 };
    }

    void Because() => _equal = SyntaxJson.StructurallyEqual(_original, _relocated);

    [Fact] void should_ignore_source_metadata() => _equal.ShouldBeTrue();
    [Fact] void should_not_serialize_the_raw_location() => SyntaxJson.Serialize(_relocated).TryGetProperty("rawLocation", out _).ShouldBeFalse();
    [Fact] void should_not_serialize_the_raw_length() => SyntaxJson.Serialize(_relocated).TryGetProperty("rawLength", out _).ShouldBeFalse();
    [Fact] void should_still_compare_the_value() => SyntaxJson.StructurallyEqual(_original, _relocated with { Value = "store" }).ShouldBeFalse();
}
