// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class with_an_unproven_predicate : given.a_generated_concept
{
    JsonElement _rule;

    void Establish() => Start("""
        concept Code : String
          generate nanoid
          validate
            rule Accepted
              ```csharp
              return true;
              ```
        """);

    void Because()
    {
        _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");
        _rule = _generation.GetProperty("rules")[0];
    }

    [Fact] void should_describe_a_predicate_rule() => _rule.GetProperty("rule").GetString().ShouldEqual("predicate");
    [Fact] void should_leave_the_predicate_unproven() => _rule.GetProperty("status").GetString().ShouldEqual("unproven");
    [Fact] void should_explain_the_unproven_predicate() => _rule.GetProperty("reason").GetString().ShouldEqual("opaque");
    [Fact] void should_report_the_compiler_warning() => _rule.GetProperty("diagnosticCode").GetString().ShouldEqual("PLAY0689");
    [Fact] void should_not_offer_a_repair() => _rule.TryGetProperty("repair", out _).ShouldBeFalse();
}
