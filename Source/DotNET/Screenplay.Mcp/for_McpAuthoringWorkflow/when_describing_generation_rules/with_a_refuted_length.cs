// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class with_a_refuted_length : given.a_generated_concept
{
    JsonElement[] _rules = [];

    void Establish() => Start("""
        concept Code : String
          generate nanoid
          validate
            matches "^[A-Za-z0-9_-]{21}$"
            length == 20
            rule Accepted
              ```csharp
              return true;
              ```
        """);

    void Because()
    {
        _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");
        _rules = _generation.GetProperty("rules").EnumerateArray().ToArray();
    }

    [Fact] void should_keep_the_rules_in_authored_order() => _rules.Select(rule => rule.GetProperty("rule").GetString()).ShouldEqual(["matches", "length", "predicate"]);
    [Fact] void should_report_the_refuted_rule_status() => _rules[1].GetProperty("status").GetString().ShouldEqual("refuted");
    [Fact] void should_report_the_authored_lines() => _rules.Select(rule => rule.GetProperty("line").GetInt32()).ShouldEqual([4, 5, 6]);
    [Fact] void should_report_the_authored_columns() => _rules.Select(rule => rule.GetProperty("column").GetInt32()).ShouldEqual([5, 5, 5]);
    [Fact] void should_report_a_witness_with_the_generated_length() => _rules[1].GetProperty("witness").GetString()!.Length.ShouldEqual(21);
    [Fact] void should_report_the_refuted_rule_diagnostic() => _rules[1].GetProperty("diagnosticCode").GetString().ShouldEqual("PLAY0688");
    [Fact] void should_offer_the_refuted_rule_repair() => _rules[1].GetProperty("repair").GetString().ShouldEqual("PLAY0688");
}
