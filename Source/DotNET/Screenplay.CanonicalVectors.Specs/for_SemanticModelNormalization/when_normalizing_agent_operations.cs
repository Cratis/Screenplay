// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Conformance;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelNormalization;

public class when_normalizing_agent_operations : Specification
{
    NormalizedSemanticModel _original;
    SemanticModelComparison _reordered;
    SemanticModelComparison _changedDescription;
    SemanticNormalizationFailed _unmapped;

    void Because()
    {
        _original = Normalize();
        _reordered = SemanticModelEquivalence.Compare(_original, Normalize(agents =>
        {
            var operations = (JsonArray)agents["operations"];
            var first = operations[0];
            operations.RemoveAt(0);
            operations.Add(first);
        }));
        _changedDescription = SemanticModelEquivalence.Compare(_original, Normalize(agents => agents["operations"][0]["description"] = "Changed description"));
        _unmapped = Catch.Exception(() => Normalize(agents => agents["operations"][0]["operation"] = $"sem1:{new string('a', 64)}")) as SemanticNormalizationFailed;
    }

    static NormalizedSemanticModel Normalize(Action<JsonNode>? change = null)
    {
        var root = JsonNode.Parse(EsmGoldenVectors.Get(48).Bytes.AsSpan());
        change?.Invoke(root["application"]["agents"]);
        return SemanticModelNormalizer.NormalizeCanonicalJson(Encoding.UTF8.GetBytes(root.ToJsonString()));
    }

    [Fact] void should_treat_the_allowlist_as_a_set() => _reordered.IsEquivalent.ShouldBeTrue();
    [Fact] void should_preserve_description_literals() => _changedDescription.IsEquivalent.ShouldBeFalse();
    [Fact]
    void should_resolve_operation_identities()
    {
        _unmapped.ShouldNotBeNull();
        _unmapped.Path.EndsWith("/operation", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact] void should_need_no_fallback_keys() => _original.FallbackKeys.ShouldBeEmpty();
}
