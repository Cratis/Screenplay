// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_reading_malformed_query_key_parts : given.a_flat_key_model
{
    [Theory]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("name")]
    [InlineData("optional")]
    [InlineData("collection")]
    public void should_reject_invalid_parts_before_comparing_the_revision(string mutation)
    {
        var compiled = Compile(Declarations);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var node = JsonNode.Parse(SemanticModelSerializer.Serialize(compiled.Value!.Model))!;
        var slice = node["application"]!["modules"]![0]!["features"]![0]!["slices"]![0]!;
        var query = ((JsonArray)slice["queries"]!).Single(query => query!["name"]!.GetValue<string>() == "Find")!;
        var parts = (JsonArray)query["keyParts"]!;
        switch (mutation)
        {
            case "duplicate": parts[1] = parts[0]!.DeepClone(); break;
            case "unknown": parts[0]!["keyProperty"] = query["id"]!.DeepClone(); break;
            case "name": parts[0]!["name"] = "other"; break;
            case "optional": parts[0]!["type"]!["optional"] = true; break;
            case "collection": parts[0]!["type"]!["collection"] = true; break;
        }
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString())));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    [Fact]
    void should_reject_several_identifiers_in_an_older_model_without_new_members()
    {
        var source = Declarations.Replace("      query Find => Row optional\n        by\n          first String\n          second String\n", string.Empty, StringComparison.Ordinal);
        var compiled = Compile(source);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(compiled.Value!.Model))
            .Replace("\"schemaVersion\":27", "\"schemaVersion\":25", StringComparison.Ordinal)
            .Replace("\"27.0\"", "\"25.0\"", StringComparison.Ordinal);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.Contains("identifier", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }
}
