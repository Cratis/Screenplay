// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class with_exact_numeric_mode : given.a_generated_concept
{
    const string PreviewReason = "Exact numeric mode is a preview (#285) that the binder does not bind; rule proofs are not established.";
    JsonElement[] _rules = [];

    void Establish() => Start("""
        numbers exact
        concept Code : String
          generate nanoid
          validate
            matches "^[A-Za-z0-9_-]{21}$"
            length == 20
            min 1
        """);

    void Because()
    {
        _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");
        _rules = _generation.GetProperty("rules").EnumerateArray().ToArray();
    }

    [Fact] void should_not_claim_a_derived_pattern() => _generation.GetProperty("derivedPattern").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_explain_the_preview_numeric_mode() => _generation.GetProperty("derivedPatternUnknownReason").GetString().ShouldEqual(PreviewReason);
    [Fact] void should_keep_all_authored_rules() => _rules.Select(rule => rule.GetProperty("rule").GetString()).ShouldEqual(["matches", "length", "minimum"]);
    [Fact] void should_leave_every_rule_unproven() => _rules.Select(rule => rule.GetProperty("status").GetString()).ShouldEqual(["unproven", "unproven", "unproven"]);
    [Fact] void should_explain_every_unproven_rule() => _rules.Select(rule => rule.GetProperty("reason").GetString()).ShouldEqual([PreviewReason, PreviewReason, PreviewReason]);
    [Fact] void should_not_claim_a_compiler_proof_diagnostic() => _rules.Any(rule => rule.TryGetProperty("diagnosticCode", out _)).ShouldBeFalse();
    [Fact] void should_not_offer_repairs() => _rules.Any(rule => rule.TryGetProperty("repair", out _)).ShouldBeFalse();
    [Fact] void should_not_invent_witnesses() => _rules.Any(rule => rule.TryGetProperty("witness", out _)).ShouldBeFalse();
    [Fact] void should_not_claim_executable_admission() => _generation.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("notAssessed");
}
