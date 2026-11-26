// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class an_unmapped_graph_projection_slice : given.graph_projection_models
{
    byte[] _json;
    Exception _error;

    void Establish()
    {
        var json = JsonNode.Parse(SemanticModelSerializer.Serialize(Compile(GraphSource)));
        json["application"]["graphProjections"][0]["declaringSlice"] = $"sem1:{new string('a', 64)}";
        _json = Encoding.UTF8.GetBytes(json.ToJsonString());
    }

    void Because() => _error = Catch.Exception(() => SemanticModelNormalizer.NormalizeCanonicalJson(_json));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<SemanticNormalizationFailed>();
    [Fact] void should_name_the_reference() => ((SemanticNormalizationFailed)_error).Path.EndsWith("/declaringSlice", StringComparison.Ordinal).ShouldBeTrue();
}
