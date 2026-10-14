// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_reading_shared_rule_consistency_vectors
{
    [Fact]
    void should_check_every_shared_consistency_vector()
    {
        var vectors = Vectors().GetProperty("consistency").EnumerateArray().ToArray();
        vectors.ShouldNotBeEmpty();
        foreach (var vector in vectors)
        {
            var primitive = vector.GetProperty("primitive").GetString() == "Uuid" ? SemanticPrimitiveType.Uuid : SemanticPrimitiveType.Text;
            var result = RuleConsistency.Check(Generation(vector), primitive, Rule(vector.GetProperty("rule")));
            result.Status.ToString().ShouldEqual(vector.GetProperty("status").GetString());
            result.Witness.ShouldEqual(Text(vector, "witness"));
            result.Reason.ShouldEqual(Text(vector, "reason"));
        }
    }

    [Fact]
    void should_check_every_shared_derived_pattern_vector()
    {
        var vectors = Vectors().GetProperty("derivedPatterns").EnumerateArray().ToArray();
        vectors.ShouldNotBeEmpty();
        foreach (var vector in vectors) DerivedPattern.Create(Generation(vector)).ShouldEqual(vector.GetProperty("derivedRegex").GetString());
    }

    static SemanticGeneration? Generation(JsonElement vector)
    {
        var generation = vector.GetProperty("generation");
        if (generation.ValueKind == JsonValueKind.Null) return null;

        return new(generation.GetProperty("kind").GetString()!)
        {
            Prefix = Text(generation, "prefix"),
            Case = Text(generation, "case"),
            Alphabet = Text(generation, "alphabet"),
            Encoding = Text(generation, "encoding"),
            Size = Number(generation, "size"),
            Count = Number(generation, "count")
        };
    }

    static SemanticValidationRule Rule(JsonElement rule)
    {
        SemanticValue? operand = null;
        if (rule.TryGetProperty("operand", out var value)) operand = value.ValueKind == JsonValueKind.Number ? new SemanticNumberValue(value.GetDecimal()) : new SemanticTextValue(value.GetString()!);

        return new(default, Enum.Parse<SemanticValidationRuleKind>(rule.GetProperty("kind").GetString()!), operand, null)
        {
            Severity = Enum.Parse<SemanticValidationSeverity>(Text(rule, "severity") ?? "Error")
        };
    }

    static string? Text(JsonElement value, string property) => value.TryGetProperty(property, out var text) ? text.GetString() : null;
    static int? Number(JsonElement value, string property) => value.TryGetProperty(property, out var number) ? number.GetInt32() : null;

    static JsonElement Vectors([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "rule-consistency.json")));

        return document.RootElement.Clone();
    }
}
