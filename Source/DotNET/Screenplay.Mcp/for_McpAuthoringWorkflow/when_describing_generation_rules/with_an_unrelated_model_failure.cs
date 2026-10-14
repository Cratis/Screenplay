// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class with_an_unrelated_model_failure : given.a_generated_concept
{
    void Establish() => Start("""
        concept Code : String
          generate nanoid
          validate
            min 1
        module Other
          feature Registration
            slice StateChange Register
              command Register
                id String identifier
                produces MissingEvent
                  for id
        """);

    void Because() => _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");

    [Fact] void should_describe_the_derived_pattern() => _generation.GetProperty("derivedPattern").GetString().ShouldEqual("^[A-Za-z0-9_-]{21}$");
    [Fact] void should_describe_the_minimum_rule() => _generation.GetProperty("rules")[0].GetProperty("rule").GetString().ShouldEqual("minimum");
    [Fact] void should_prove_the_rule_independently() => _generation.GetProperty("rules")[0].GetProperty("status").GetString().ShouldEqual("proven");
    [Fact] void should_not_claim_executable_admission() => _generation.GetProperty("executable").GetProperty("status").GetString().ShouldEqual("notAssessed");
}
