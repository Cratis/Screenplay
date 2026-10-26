// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class fallback_segments : Specification
{
    NormalizedSemanticModel _normalized;
    NormalizedSemanticModel _reordered;

    void Because()
    {
        var first = $$"""{"name":"Duplicate","value":"sem1:{{new string('a', 64)}}"}""";
        var second = $$"""{"name":"Duplicate","value":"sem1:{{new string('b', 64)}}"}""";
        var unnamed = $$"""{"value":"rev1:{{new string('a', 64)}}"}""";
        var otherUnnamed = $$"""{"value":"catrev1:{{new string('b', 64)}}"}""";
        _normalized = Normalize($$"""{"application":{"policies":[{{second}},{{unnamed}},{{first}},{{otherUnnamed}}]} }""");
        _reordered = Normalize($$"""{"application":{"policies":[{{otherUnnamed}},{{first}},{{unnamed}},{{second}}]} }""");
    }

    [Fact] void should_preserve_identity_looking_literals_when_ranking_duplicate_and_missing_names() => _normalized.Text.ShouldEqual("""{"application":{"policies":{"[Duplicate~1]":{"name":"Duplicate","value":"sem1:AAAA"},"[Duplicate~2]":{"name":"Duplicate","value":"sem1:BBBB"},"[~1]":{"value":"catrev1:BBBB"},"[~2]":{"value":"rev1:AAAA"}}}}""".Replace("AAAA", new string('a', 64), StringComparison.Ordinal).Replace("BBBB", new string('b', 64), StringComparison.Ordinal));
    [Fact] void should_sort_fallback_paths_ordinally() => Assert.Equal(new[] { "/application/policies[Duplicate~1]", "/application/policies[Duplicate~2]", "/application/policies[~1]", "/application/policies[~2]" }, _normalized.FallbackKeys);
    [Fact] void should_keep_normalized_text_after_reordering() => _reordered.Text.ShouldEqual(_normalized.Text);
    [Fact] void should_keep_fallback_paths_after_reordering() => Assert.Equal(_normalized.FallbackKeys, _reordered.FallbackKeys);

    static NormalizedSemanticModel Normalize(string json) => SemanticModelNormalizer.NormalizeCanonicalJson(Encoding.UTF8.GetBytes(json));
}
