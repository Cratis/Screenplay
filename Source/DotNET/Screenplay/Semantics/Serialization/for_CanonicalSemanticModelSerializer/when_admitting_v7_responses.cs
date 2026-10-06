// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_admitting_v7_responses : Specification
{
    Exception _error;
    bool _language;
    bool _semantics;

    void Because()
    {
        _language = LanguageVersion.TryParse("7.0", out _);
        _semantics = SemanticVersion.TryParse("7.0", out _);
        var root = JsonNode.Parse(canonical_serialization_golden_vectors.SemanticModelV6Bytes);
        root["schemaVersion"] = 7;
        root["languageVersion"] = "7.0";
        root["semanticVersion"] = "7.0";
        var commands = root["application"]["modules"][0]["features"]
            .AsArray().SelectMany(feature => feature["slices"].AsArray())
            .SelectMany(slice => slice["commands"].AsArray());
        var command = commands.First(value => value["properties"].AsArray().Count > 0);
        var property = command["properties"][0];
        command["response"] = new JsonObject
        {
            ["kind"] = "scalar",
            ["source"] = property["id"].DeepClone(),
            ["type"] = property["type"].DeepClone()
        };

        // A recognized, valid response must reach revision checking, rather than version/member refusal.
        _error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString())));
    }

    [Fact] void should_parse_the_language_version() => _language.ShouldBeTrue();
    [Fact] void should_parse_the_semantic_version() => _semantics.ShouldBeTrue();
    [Fact] void should_read_the_response_before_checking_the_revision() => _error.Message.Contains("computed revision", StringComparison.Ordinal).ShouldBeTrue();
}
