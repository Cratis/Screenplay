// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_comparing_fits_slot_source_metadata : Specification
{
    ScreenTemplateSyntax _template;
    ScreenTemplateSyntax _relocated;

    void Establish()
    {
        _template = new ScreenTemplateSyntax("Shell", [], SourceLocation.Start, "content")
        {
            FitsSlotLocation = new SourceLocation(3, 5)
        };
        _relocated = _template with { FitsSlotLocation = new SourceLocation(20, 7) };
    }

    [Fact] void should_not_serialize_the_anchor() => SyntaxJson.Serialize(_template).TryGetProperty("fitsSlotLocation", out _).ShouldBeFalse();
    [Fact] void should_compare_the_same_as_a_template_without_the_anchor() => SyntaxJson.StructurallyEqual(_template, _relocated with { FitsSlotLocation = null }).ShouldBeTrue();
    [Fact] void should_compare_fits_slot_names() => SyntaxJson.StructurallyEqual(_template, _relocated with { FitsSlot = "sidebar" }).ShouldBeFalse();
    [Fact] void should_not_advertise_the_anchor_in_the_schema() => SyntaxSchema.For(nameof(ScreenTemplateSyntax)).GetProperty("properties").TryGetProperty("fitsSlotLocation", out _).ShouldBeFalse();
    [Fact] void should_not_advertise_a_directive_as_a_kind() => SyntaxSchema.Kinds.ShouldNotContain("FitsSlotSyntax");
    [Fact] void should_keep_the_original_typed_json_members() => SyntaxJson.Serialize(_template).GetRawText().ShouldEqual("{\"kind\":\"ScreenTemplateSyntax\",\"arrangement\":null,\"behaviors\":[],\"fitsSlot\":\"content\",\"name\":\"Shell\",\"slots\":[],\"usedBehaviors\":[]}");
}
