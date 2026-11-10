// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_serializing_nested_sample_rows : Specification
{
    SampleDataFieldSyntax _original;
    SampleDataFieldSyntax _roundtrip;
    string _json;
    string _scalarJson;
    string _emptyJson;

    void Establish()
    {
        var children = new SampleDataFieldSyntax("children", SampleDataValueKind.Rows, null, SourceLocation.Start)
        {
            Rows = [new([new("sku", SampleDataValueKind.Text, "PEN", SourceLocation.Start)], SourceLocation.Start)]
        };
        _original = new("lines", SampleDataValueKind.Rows, null, SourceLocation.Start)
        {
            Rows = [new([new("sku", SampleDataValueKind.Text, "BOOK", SourceLocation.Start), children], SourceLocation.Start)]
        };
    }

    void Because()
    {
        var serialized = SyntaxJson.Serialize(_original);
        _json = serialized.GetRawText();
        _roundtrip = (SampleDataFieldSyntax)SyntaxJson.Deserialize(serialized);
        _scalarJson = SyntaxJson.Serialize(new SampleDataFieldSyntax("sku", SampleDataValueKind.Text, "BOOK", SourceLocation.Start)).GetRawText();
        _emptyJson = SyntaxJson.Serialize(new SampleDataFieldSyntax("lines", SampleDataValueKind.Rows, null, SourceLocation.Start)).GetRawText();
    }

    [Fact] void should_round_trip_nested_rows() => SyntaxJson.StructurallyEqual(_original, _roundtrip).ShouldBeTrue();
    [Fact] void should_serialize_the_kind_by_name() => _json.ShouldContain("\"syntaxKind\":\"Rows\"");
    [Fact] void should_preserve_recursive_children() => _roundtrip.Rows.Single().Fields.Last().Rows.Single().Fields.Single().Value.ShouldEqual("PEN");
    [Fact] void should_omit_rows_for_a_scalar() => _scalarJson.ShouldNotContain("\"rows\"");
    [Fact] void should_omit_rows_for_an_empty_collection() => _emptyJson.ShouldNotContain("\"rows\"");
    [Fact] void should_default_missing_rows_to_empty() => ((SampleDataFieldSyntax)SyntaxJson.Deserialize(System.Text.Json.JsonElement.Parse(_emptyJson))).Rows.ShouldBeEmpty();
}
