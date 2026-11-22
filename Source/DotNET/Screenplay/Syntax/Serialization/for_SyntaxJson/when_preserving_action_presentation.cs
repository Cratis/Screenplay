// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_preserving_action_presentation : Specification
{
    ScreenActionSyntax _action;
    ScreenActionSyntax _restored;
    JsonObject _wire;

    void Establish() => _action = new("Retry", "Retry", null, SourceLocation.Start)
    {
        Subject = ScreenActionSubjectKind.Item,
        Presentation = [new("placement", "row-menu", SourceLocation.Start), new("extension", "future", SourceLocation.Start)]
    };

    void Because()
    {
        _wire = JsonNode.Parse(SyntaxJson.Serialize(_action).GetRawText())!.AsObject();
        _restored = (ScreenActionSyntax)SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(_wire));
    }

    [Fact] void should_round_trip_authored_values() => SyntaxJson.StructurallyEqual(_action, _restored).ShouldBeTrue();
    [Fact] void should_omit_empty_presentation() => SyntaxJson.Serialize(_action with { Presentation = [] }).TryGetProperty("presentation", out _).ShouldBeFalse();
    [Fact] void should_default_an_older_action_to_empty() => ((ScreenActionSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_action with { Presentation = [] }))).Presentation.ShouldBeEmpty();

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("\"row-menu\"")]
    [InlineData("[null]")]
    void should_reject_non_collection_or_non_value_shapes(string value)
    {
        _wire["presentation"] = JsonNode.Parse(value);
        Catch.Exception(() => SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(_wire))).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    [Theory]
    [InlineData("name", "\"\"")]
    [InlineData("value", "\" \"")]
    [InlineData("value", "null")]
    [InlineData("value", "42")]
    void should_reject_invalid_values(string member, string value)
    {
        _wire["presentation"]![0]![member] = JsonNode.Parse(value);
        Catch.Exception(() => SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(_wire))).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    [Fact]
    void should_reject_duplicate_keys()
    {
        _wire["presentation"]![1]!["name"] = "placement";
        Catch.Exception(() => SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(_wire))).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    [Fact]
    void should_reject_duplicate_typed_values() => Catch.Exception(() => SyntaxJson.Deserialize(SyntaxJson.Serialize(_action with { Presentation = [.. _action.Presentation, _action.Presentation.First()] }))).ShouldBeOfExactType<InvalidSyntaxJson>();

    [Fact]
    void should_serialize_duplicate_typed_values_without_throwing() => Catch.Exception(() => SyntaxJson.Serialize(_action with { Presentation = [.. _action.Presentation, _action.Presentation.First()] })).ShouldBeNull();
}
