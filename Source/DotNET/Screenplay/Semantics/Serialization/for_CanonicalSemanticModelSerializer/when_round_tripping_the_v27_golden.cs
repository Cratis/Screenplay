// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v27_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;

    void Because()
    {
        _bytes = canonical_serialization_golden_vectors.EsmV27Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_preserve_the_language_version() => _model.LanguageVersion.ShouldEqual(LanguageVersion.V27);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
    [Fact] void should_select_the_composite_key_version() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V27);
    [Fact] void should_preserve_earlier_support() => EsmSchemaV25Support.Supports(LanguageVersion.V27, SemanticVersion.V27).ShouldBeFalse();
    [Fact] void should_write_untyped_parts_keys() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"kind\":\"parts\",\"parts\":[");
    [Fact] void should_write_query_key_parts() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"keyParts\":[");

    [Theory]
    [InlineData("from", false)]
    [InlineData("from", true)]
    [InlineData("removedWith", false)]
    [InlineData("removedWith", true)]
    public void should_refuse_incomplete_or_duplicate_projection_parts(string block, bool duplicate)
    {
        var node = JsonNode.Parse(_bytes)!;
        var parts = (JsonArray)Slice(node)["projections"]![0]!["scope"]![block]![0]!["key"]!["parts"]!;
        if (duplicate) parts[1] = parts[0]!.DeepClone();
        else parts.RemoveAt(0);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString())));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_an_incomplete_query_key()
    {
        var node = JsonNode.Parse(_bytes)!;
        var query = ((JsonArray)Slice(node)["queries"]!).Single(query => query!["name"]!.GetValue<string>() == "Total")!;
        ((JsonArray)query["keyParts"]!).RemoveAt(0);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString())));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_new_members_before_the_admitting_version()
    {
        var json = Encoding.UTF8.GetString(_bytes)
            .Replace($"\"schemaVersion\":{CompositeReadModelKeyVersion.Major}", "\"schemaVersion\":25", StringComparison.Ordinal)
            .Replace($"\"{CompositeReadModelKeyVersion.Major}.0\"", "\"25.0\"", StringComparison.Ordinal);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    static JsonNode Slice(JsonNode node) => node["application"]!["modules"]![0]!["features"]![0]!["slices"]![0]!;
}
