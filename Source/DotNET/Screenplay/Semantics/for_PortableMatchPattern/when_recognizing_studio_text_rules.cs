// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

namespace Cratis.Screenplay.Semantics.for_PortableMatchPattern;

public class when_recognizing_studio_text_rules
{
    [Fact]
    void should_pin_every_v1_code_unit_to_real_regex_escape()
    {
        var value = new string([.. Enumerable.Range(0, char.MaxValue + 1).Select(value => (char)value)]);
        LegacyTextRulePatterns.Escape(value, true).ShouldEqual(Regex.Escape(value));
    }

    [Fact]
    void should_recognize_every_shared_shape_and_execute_old_esm_identically()
    {
        using var document = PortableMatchVectors.Read("legacy-text-rule-shapes.json");
        var vectors = document.RootElement.GetProperty("literals").EnumerateArray().ToArray();
        vectors.Length.ShouldEqual(16);
        foreach (var vector in vectors)
        {
            var value = Text(vector, "value");
            foreach (var v1 in new[] { true, false })
            {
                var literal = Text(vector, v1 ? "v1" : "v2");
                LegacyTextRulePatterns.Escape(value, v1).ShouldEqual(literal);
                var end = v1 ? "$" : @"(?![\s\S])";
                var shapes = new List<(string Pattern, SemanticValidationRuleKind Kind)>
                {
                    (@"^(?![\s\S]*" + literal + ")", SemanticValidationRuleKind.NotContains),
                    ("^(?!" + literal + end + ")", SemanticValidationRuleKind.NotEqual)
                };
                if (!v1)
                {
                    shapes.Add(("^" + literal + end, SemanticValidationRuleKind.Equal));
                    shapes.Add((literal + end, SemanticValidationRuleKind.EndsWith));
                }
                foreach (var (pattern, kind) in shapes)
                {
                    var check = PortableMatchPattern.Check(pattern);
                    check.Status.ShouldEqual(PortableMatchStatus.Deprecated);
                    check.Code.ShouldEqual("PLAY0787");
                    check.Replacement.ShouldEqual(new PortableMatchReplacement(kind, value));
                    var old = new SemanticValidationRule(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null);
                    var replacement = new SemanticValidationRule(default, kind, SemanticValue.Text(value), null);
                    SemanticValidationRules.UnsupportedPattern(old).ShouldBeNull();
                    foreach (var input in new[] { "", value, "a" + value, value + "\n", "a" + value + "b", "雪", "\ud800" })
                    {
                        SemanticValidationRules.Satisfies(old, SemanticValue.Text(input)).ShouldEqual(SemanticValidationRules.Satisfies(replacement, SemanticValue.Text(input)));
                    }
                }
            }
        }
    }

    static string Text(System.Text.Json.JsonElement vector, string property) =>
        vector.TryGetProperty("codeUnits", out var codes)
            ? new string([.. codes.EnumerateArray().Select(code => (char)code.GetInt32())])
            : vector.GetProperty(property).GetString()!;

    [Fact]
    void should_refuse_near_misses_and_leave_admitted_nonlookaround_patterns_alone()
    {
        using var document = PortableMatchVectors.Read("legacy-text-rule-shapes.json");
        foreach (var pattern in document.RootElement.GetProperty("refused").EnumerateArray())
        {
            LegacyTextRulePatterns.Recognize(pattern.GetString()!).ShouldBeNull();
            PortableMatchPattern.Check(pattern.GetString()!).Code.ShouldEqual("PLAY0699");
        }
        foreach (var pattern in document.RootElement.GetProperty("unchanged").EnumerateArray()) PortableMatchPattern.Check(pattern.GetString()!).Status.ShouldEqual(PortableMatchStatus.Admitted);
    }
}
