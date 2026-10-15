// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_reading_shared_numeric_rule_vectors : Specification
{
    JsonElement[] _vectors;
    RuleConsistencyResult[] _results;

    void Establish()
    {
        var root = Root();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Source", "Screenplay", "Compiler", "Conformance", "generation-numeric-rules.json")));
        _vectors = [.. document.RootElement.GetProperty("rules").EnumerateArray().Select(vector => vector.Clone())];
    }

    void Because() => _results = [.. _vectors.Select(vector => RuleConsistency.CheckRandom(vector.GetProperty("range").GetString()!, Rule(vector)))];

    [Fact] void should_check_a_nonempty_proof_population() => _vectors.ShouldNotBeEmpty();
    [Fact] void should_compare_exact_decimal_bounds_with_exact_int64_endpoints() => _results.Select(result => result.Status.ToString()).ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("status").GetString())]);
    [Fact] void should_return_only_genuine_integer_witnesses() => _results.Select(result => result.Witness).ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("witness").GetString())]);
    [Fact] void should_preserve_the_reason_for_unproven_rules() => _results.Select(result => result.Reason).ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("reason").GetString())]);

    static SemanticValidationRule Rule(JsonElement vector)
    {
        var operand = vector.TryGetProperty("operand", out var value) ? SemanticValue.Number(decimal.Parse(value.GetString()!, CultureInfo.InvariantCulture)) : null;

        return new(default, Enum.Parse<SemanticValidationRuleKind>(vector.GetProperty("kind").GetString()!), operand, null);
    }

    static string Root([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;

        return root!.FullName;
    }
}
