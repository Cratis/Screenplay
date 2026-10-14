// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class with_a_reserved_sequence : given.a_generated_concept
{
    JsonElement _rule;

    void Establish() => Start("concept Code : String\n  generate sequence\n  validate\n    not empty\n");

    void Because()
    {
        _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");
        _rule = _generation.GetProperty("rules")[0];
    }

    [Fact] void should_not_invent_a_pattern() => _generation.GetProperty("derivedPattern").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_explain_the_unknown_pattern() => _generation.GetProperty("derivedPatternUnknownReason").GetString().ShouldNotBeEmpty();
    [Fact] void should_describe_the_authored_rule() => _rule.GetProperty("rule").GetString().ShouldEqual("notEmpty");
    [Fact] void should_leave_the_rule_unproven() => _rule.GetProperty("status").GetString().ShouldEqual("unproven");
    [Fact] void should_explain_the_unproven_rule() => _rule.GetProperty("reason").GetString().ShouldNotBeEmpty();
    [Fact] void should_not_offer_a_repair() => _rule.TryGetProperty("repair", out _).ShouldBeFalse();
    [Fact] void should_not_claim_a_compiler_proof_diagnostic() => _rule.TryGetProperty("diagnosticCode", out _).ShouldBeFalse();
}
