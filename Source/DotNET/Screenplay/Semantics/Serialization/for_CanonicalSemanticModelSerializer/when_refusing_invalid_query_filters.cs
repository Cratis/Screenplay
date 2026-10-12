// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_refusing_invalid_query_filters : Specification
{
    JsonObject _json;

    void Establish() => _json = JsonNode.Parse(SemanticModelSerializer.Serialize(query_filter_source.Compile().Model))!.AsObject();

    [Theory]
    [InlineData("unknown")]
    [InlineData("repeated")]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("type")]
    void should_refuse_invalid_supplied_values(string kind)
    {
        var result = Slice["specifications"]!.AsArray().Select(node => node!.AsObject()).First(specification => specification["name"]!.GetValue<string>() == "ReversedRows")["thenQueries"]![0]!;
        var filters = result["filters"]!.AsArray();
        switch (kind)
        {
            case "unknown": filters[0]!["filter"] = Slice["id"]!.GetValue<string>(); break;
            case "repeated": filters.Add(filters[0]!.DeepClone()); break;
            case "missing": filters.Clear(); break;
            case "null": filters[0]!["value"] = JsonNode.Parse("{\"kind\":\"null\"}"); break;
            case "type": filters[0]!["value"] = JsonNode.Parse("{\"kind\":\"boolean\",\"value\":true}"); break;
        }
        Catch.Exception(() => SemanticModelSerializer.Deserialize(System.Text.Encoding.UTF8.GetBytes(_json.ToJsonString()))).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_filters_below_v14()
    {
        _json["schemaVersion"] = 13;
        _json["languageVersion"] = "13.0";
        _json["semanticVersion"] = "13.0";
        Catch.Exception(() => SemanticModelSerializer.Deserialize(System.Text.Encoding.UTF8.GetBytes(_json.ToJsonString()))).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Theory]
    [InlineData("property")]
    [InlineData("name")]
    [InlineData("duplicate")]
    [InlineData("type")]
    [InlineData("collection")]
    [InlineData("cardinality")]
    void should_refuse_invalid_declarations(string kind)
    {
        var query = Slice["queries"]!.AsArray().First(node => node!["name"]!.GetValue<string>() == "Filtered")!;
        var filters = query["filters"]!.AsArray();
        var filter = filters[0]!;
        switch (kind)
        {
            case "property": filter["property"] = query["id"]!.GetValue<string>(); break;
            case "name": filter["name"] = "unknown"; break;
            case "duplicate": filters[1]!["name"] = filter["name"]!.GetValue<string>(); filters[1]!["property"] = filter["property"]!.GetValue<string>(); break;
            case "type": filter["type"]!["primitive"] = "boolean"; break;
            case "collection": filter["type"]!["collection"] = true; break;
            case "cardinality": query["cardinality"] = "zeroOrOne"; break;
        }
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(System.Text.Encoding.UTF8.GetBytes(_json.ToJsonString())));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error!.Message.ShouldContain("filter");
    }

    JsonObject Slice => _json["application"]!["modules"]![0]!["features"]![0]!["slices"]![0]!.AsObject();
}
