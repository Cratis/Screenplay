// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class an_unmapped_identity : given.compiled_models
{
    static readonly string _unmapped = $"sem1:{new string('a', 64)}";

    byte[] _json;
    Exception _error;

    void Establish()
    {
        var canonical = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(Compile(Source)));
        var json = JsonNode.Parse(canonical);
        var produced = json["application"]["modules"].AsArray()
            .SelectMany(module => module["features"].AsArray())
            .SelectMany(feature => feature["slices"].AsArray())
            .SelectMany(slice => slice["commands"].AsArray())
            .SelectMany(command => command["produces"].AsArray()).First();
        produced["eventContract"] = _unmapped;
        _json = Encoding.UTF8.GetBytes(json.ToJsonString());
    }

    void Because() => _error = Catch.Exception(() => SemanticModelNormalizer.NormalizeCanonicalJson(_json));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<SemanticNormalizationFailed>();
    [Fact] void should_name_the_path() => ((SemanticNormalizationFailed)_error).Path.EndsWith("/eventContract", StringComparison.Ordinal).ShouldBeTrue();
}
