// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_proving_generated_checksums : Specification
{
    JsonElement[] _vectors;
    RuleConsistencyResult[] _results;

    void Establish()
    {
        var root = Root();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Source", "Screenplay", "Compiler", "Conformance", "generation-checksum-rules.json")));
        _vectors = [.. document.RootElement.GetProperty("proof").EnumerateArray().Select(vector => vector.Clone())];
    }

    void Because() => _results = [.. _vectors.Select(vector => RuleConsistency.CheckPattern(
        vector.GetProperty("pattern").GetString()!,
        vector.GetProperty("checksum").GetString(),
        new(default, SemanticValidationRuleKind.Checksum, SemanticValue.Text(vector.GetProperty("algorithm").GetString()!), null)))];

    [Fact] void should_check_a_nonempty_proof_population() => _vectors.ShouldNotBeEmpty();
    [Fact] void should_prove_only_same_algorithm_checksums_by_construction() => _results.Select(result => result.Status.ToString()).ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("status").GetString())]);
    [Fact] void should_never_fabricate_an_arithmetic_counterexample() => _results.ShouldEachConformTo(result => result.Status != RuleConsistencyStatus.Refuted);

    static string Root([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;

        return root!.FullName;
    }
}
