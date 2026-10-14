// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_reading_shared_pattern_rule_vectors : Specification
{
    JsonElement[] _vectors;
    RuleConsistencyResult[] _results;

    void Establish()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance", "generation-pattern-rules.json")));
        _vectors = [.. document.RootElement.GetProperty("rules").EnumerateArray().Select(vector => vector.Clone())];
    }

    void Because() => _results = [.. _vectors.Select(vector => RuleConsistency.CheckPattern(
        vector.GetProperty("pattern").GetString()!,
        vector.TryGetProperty("checksum", out var checksum) ? checksum.GetString() : null,
        Rule(vector),
        vector.TryGetProperty("date", out var date) ? DateOnly.ParseExact(date.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture) : null))];

    [Fact] void should_check_a_nonempty_proof_population() => _vectors.ShouldNotBeEmpty();
    [Fact] void should_agree_on_proof_status() => _results.Select(result => result.Status.ToString()).ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("status").GetString())]);
    [Fact] void should_agree_on_genuine_support_witnesses() => _results.Select(result => result.Witness).ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("witness").GetString())]);
    [Fact] void should_preserve_unproven_reasons() => _results.Select(result => result.Reason).ShouldEqual([.. _vectors.Select(vector => vector.GetProperty("reason").GetString())]);

    static SemanticValidationRule Rule(JsonElement vector)
    {
        var operand = Operand(vector);

        return new(default, Enum.Parse<SemanticValidationRuleKind>(vector.GetProperty("kind").GetString()!), operand, null);
    }

    static SemanticValue? Operand(JsonElement vector)
    {
        if (!vector.TryGetProperty("operand", out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number) return SemanticValue.Number(value.GetDecimal());
        if (vector.TryGetProperty("operandKind", out var kind) && kind.GetString() == "number") return SemanticValue.Number(decimal.Parse(value.GetString()!, CultureInfo.InvariantCulture));

        return SemanticValue.Text(value.GetString()!);
    }

    static string Root([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;

        return root!.FullName;
    }
}
