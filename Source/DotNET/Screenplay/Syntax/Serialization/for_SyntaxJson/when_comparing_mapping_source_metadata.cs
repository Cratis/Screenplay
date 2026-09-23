// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_comparing_mapping_source_metadata : Specification
{
    PropertyMappingSyntax _original;
    PropertyMappingSyntax _relocated;
    bool _equal;

    void Establish()
    {
        _original = new PropertyMappingSyntax("name", new PathExpressionSyntax("name", SourceLocation.Start), SourceLocation.Start);
        _relocated = _original with
        {
            Location = new SourceLocation(12, 5, "other.play"),
            SourceLocation = new SourceLocation(12, 12, "other.play"),
            SourceLength = 4
        };
    }

    void Because() => _equal = SyntaxJson.StructurallyEqual(_original, _relocated);

    [Fact] void should_ignore_source_metadata() => _equal.ShouldBeTrue();
    [Fact] void should_not_serialize_the_source_location() => SyntaxJson.Serialize(_relocated).TryGetProperty("sourceLocation", out _).ShouldBeFalse();
    [Fact] void should_not_serialize_the_source_length() => SyntaxJson.Serialize(_relocated).TryGetProperty("sourceLength", out _).ShouldBeFalse();
    [Fact] void should_still_compare_the_mapped_property() => SyntaxJson.StructurallyEqual(_original, _relocated with { Property = "other" }).ShouldBeFalse();
}
