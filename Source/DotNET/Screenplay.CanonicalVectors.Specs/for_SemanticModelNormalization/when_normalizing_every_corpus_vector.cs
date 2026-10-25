// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Conformance;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelNormalization;

public class when_normalizing_every_corpus_vector : Specification
{
    (string Name, byte[] Bytes)[] _models;
    List<string> _failures;
    List<string> _nondeterministic;
    List<string> _fallbacks;

    void Establish()
    {
        var vectors = typeof(CanonicalCorpusVector).Assembly.GetExportedTypes()
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(property => property.PropertyType == typeof(CanonicalCorpusVector))
            .Select(property => (Name: $"{property.DeclaringType!.Name}.{property.Name}", Vector: (CanonicalCorpusVector)property.GetValue(null)!))
            .Where(_ => !_.Vector.EsmBytes.IsDefaultOrEmpty)
            .Select(_ => (_.Name, Bytes: _.Vector.EsmBytes.ToArray()));
        var goldens = EsmGoldenVectors.Versions.Select(version => ($"EsmGoldenVectors.V{version}", EsmGoldenVectors.Get(version).Bytes.ToArray()));
        _models = [.. vectors, .. goldens, ("EsmGoldenVectors.PolicyNegationV7", EsmGoldenVectors.PolicyNegationV7.Bytes.ToArray())];
        _failures = [];
        _nondeterministic = [];
        _fallbacks = [];
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
            }
            catch (SemanticNormalizationFailed failure)
            {
                _failures.Add($"{name}: {failure.Message}");
            }
        }
    }

    [Fact] void should_normalize_corpus_vectors() => _models.Count(_ => !_.Name.StartsWith("EsmGoldenVectors.", StringComparison.Ordinal)).ShouldBeGreaterThan(30);
    [Fact] void should_normalize_every_esm_golden() => _models.Count(_ => _.Name.StartsWith("EsmGoldenVectors.", StringComparison.Ordinal)).ShouldEqual(EsmGoldenVectors.Versions.Length + 1);
    [Fact] void should_classify_every_array_and_map_every_identity() => Assert.True(_failures.Count == 0, string.Join('\n', _failures));
    [Fact] void should_normalize_deterministically() => Assert.True(_nondeterministic.Count == 0, string.Join('\n', _nondeterministic));
    [Fact] void should_need_no_fallback_keys() => Assert.True(_fallbacks.Count == 0, string.Join('\n', _fallbacks));
}
