// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_comparing_source_metadata : Specification
{
    SliceSyntax _original;
    SliceSyntax _relocated;
    bool _equal;

    void Establish()
    {
        _original = (SliceSyntax)given.syntax_examples.Create(typeof(SliceSyntax));
        _relocated = _original with
        {
            Location = new SourceLocation(50, 20, "other.play"),
            DescriptionLocation = new SourceLocation(51, 5, "other.play"),
            DescriptionRawLength = 87
        };
    }

    void Because() => _equal = SyntaxJson.StructurallyEqual(_original, _relocated);

    [Fact] void should_ignore_source_metadata() => _equal.ShouldBeTrue();
    [Fact] void should_not_serialize_description_offsets() => SyntaxJson.Serialize(_relocated).TryGetProperty("descriptionLocation", out _).ShouldBeFalse();
    [Fact] void should_not_serialize_raw_source_lengths() => SyntaxJson.Serialize(_relocated).TryGetProperty("descriptionRawLength", out _).ShouldBeFalse();
    [Fact] void should_still_compare_description_text() => SyntaxJson.StructurallyEqual(_original, _relocated with { Description = "changed" }).ShouldBeFalse();
}
