// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class with_a_proven_match : given.a_generated_concept
{
    JsonElement _rule;

    void Establish() => Start("""
        concept Code : String
          generate nanoid
          validate
            matches "^[A-Za-z0-9_-]{21}$"
        """);

    void Because()
    {
        _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");
        _rule = _generation.GetProperty("rules")[0];
    }

    [Fact] void should_describe_the_derived_pattern() => _generation.GetProperty("derivedPattern").GetString().ShouldEqual("^[A-Za-z0-9_-]{21}$");
    [Fact] void should_omit_the_unknown_pattern_reason() => _generation.TryGetProperty("derivedPatternUnknownReason", out _).ShouldBeFalse();
    [Fact] void should_describe_the_match_rule() => _rule.GetProperty("rule").GetString().ShouldEqual("matches");
    [Fact] void should_report_the_proven_rule_status() => _rule.GetProperty("status").GetString().ShouldEqual("proven");
    [Fact] void should_omit_a_diagnostic_for_the_proven_rule() => _rule.TryGetProperty("diagnosticCode", out _).ShouldBeFalse();
    [Fact] void should_omit_a_repair_for_the_proven_rule() => _rule.TryGetProperty("repair", out _).ShouldBeFalse();
}
