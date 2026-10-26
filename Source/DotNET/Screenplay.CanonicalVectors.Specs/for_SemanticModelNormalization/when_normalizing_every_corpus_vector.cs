// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Conformance;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelNormalization;

public class when_normalizing_every_corpus_vector : Specification
{
    (string Name, byte[] Bytes)[] _models;
    List<string> _failures;
    List<string> _nondeterministic;
    List<string> _fallbacks;
    List<string> _reassigned;
    int _identityChecks;

    void Establish()
    {
        var vectors = typeof(CanonicalCorpusVector).Assembly.GetExportedTypes()
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(property => property.PropertyType == typeof(CanonicalCorpusVector))
            .Select(property => (Name: $"{property.DeclaringType.Name}.{property.Name}", Vector: (CanonicalCorpusVector)property.GetValue(null)))
            .Where(_ => !_.Vector.EsmBytes.IsDefaultOrEmpty)
            .Select(_ => (_.Name, Bytes: _.Vector.EsmBytes.ToArray()));
        var goldens = EsmGoldenVectors.Versions.Select(version => ($"EsmGoldenVectors.V{version}", EsmGoldenVectors.Get(version).Bytes.ToArray()));
        _models = [.. vectors, .. goldens, ("EsmGoldenVectors.PolicyNegationV7", EsmGoldenVectors.PolicyNegationV7.Bytes.ToArray())];
        _failures = [];
        _nondeterministic = [];
        _fallbacks = [];
        _reassigned = [];
    }

    void Because()
    {
        foreach (var (name, bytes) in _models)
        {
            try
            {
                var first = SemanticModelNormalizer.NormalizeCanonicalJson(bytes);
                var second = SemanticModelNormalizer.NormalizeCanonicalJson(bytes.ToArray());
                if (!string.Equals(first.Text, second.Text, StringComparison.Ordinal))
                {
                    _nondeterministic.Add(name);
                }

                _fallbacks.AddRange(first.FallbackKeys.Select(key => $"{name}: {key}"));
                var reassigned = SemanticModelNormalizer.NormalizeCanonicalJson(ReassignIdentities(bytes));
                _identityChecks++;
                var comparison = SemanticModelEquivalence.Compare(first, reassigned);
                if (!comparison.IsEquivalent) _reassigned.Add($"{name}: {string.Join(';', comparison.Differences.Take(5))}");
            }
            catch (SemanticNormalizationFailed failure)
            {
                _failures.Add($"{name}: {failure.Message}");
            }
        }
    }

    // A metamorphic check independent of the string classifier: reverse definitions within each identity
    // family, then substitute exact matches throughout the writer's document (including every reference).
    static byte[] ReassignIdentities(byte[] bytes)
    {
        var root = JsonNode.Parse(bytes);
        var definitions = Descendants(root).OfType<JsonObject>()
            .SelectMany(node => (string?[])[node["id"]?.GetValue<string>(), node["contractId"]?.GetValue<string>()])
            .OfType<string>().Distinct(StringComparer.Ordinal);
        var replacements = definitions.GroupBy(id => id.Split(':')[0], StringComparer.Ordinal)
            .SelectMany(group => group.Order(StringComparer.Ordinal).Zip(group.OrderDescending(StringComparer.Ordinal)))
            .ToDictionary(pair => pair.First, pair => pair.Second, StringComparer.Ordinal);
        return Replace(bytes, replacements);
    }

    static IEnumerable<JsonNode> Descendants(JsonNode node) =>
        ((JsonNode[])[node]).Concat(node switch
        {
            JsonObject obj => obj.SelectMany(member => member.Value is null ? [] : Descendants(member.Value)),
            JsonArray array => array.SelectMany(item => item is null ? [] : Descendants(item)),
            _ => [],
        });

    static byte[] Replace(byte[] bytes, Dictionary<string, string> replacements)
    {
        // Preserve every other byte, including each writer call's own escaping. Re-serializing even with
        // one shared encoder would alter the mixture of fixed ASCII text and runtime-encoded schema tokens.
        using var output = new MemoryStream();
        var reader = new Utf8JsonReader(bytes);
        var offset = 0;
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.String || !replacements.TryGetValue(reader.GetString(), out var replacement)) continue;
            var start = (int)reader.TokenStartIndex;
            output.Write(bytes.AsSpan(offset, start - offset));
            output.Write(Encoding.UTF8.GetBytes($"\"{replacement}\""));
            offset = (int)reader.BytesConsumed;
        }

        output.Write(bytes.AsSpan(offset));
        return output.ToArray();
    }

    [Fact] void should_check_reassignment_for_every_model() => _identityChecks.ShouldEqual(_models.Length);
    [Fact] void should_preserve_meaning_under_reversed_identity_assignments() => Assert.True(_reassigned.Count == 0, string.Join('\n', _reassigned));
    [Fact] void should_normalize_corpus_vectors() => _models.Count(_ => !_.Name.StartsWith("EsmGoldenVectors.", StringComparison.Ordinal)).ShouldBeGreaterThan(30);
    [Fact] void should_normalize_every_esm_golden() => _models.Count(_ => _.Name.StartsWith("EsmGoldenVectors.", StringComparison.Ordinal)).ShouldEqual(EsmGoldenVectors.Versions.Length + 1);
    [Fact] void should_classify_every_array_and_map_every_identity() => Assert.True(_failures.Count == 0, string.Join('\n', _failures));
    [Fact] void should_normalize_deterministically() => Assert.True(_nondeterministic.Count == 0, string.Join('\n', _nondeterministic));
    [Fact] void should_need_no_fallback_keys() => Assert.True(_fallbacks.Count == 0, string.Join('\n', _fallbacks));
}
