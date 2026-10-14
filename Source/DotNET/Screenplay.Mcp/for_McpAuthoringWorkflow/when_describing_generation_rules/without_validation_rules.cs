// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class without_validation_rules : given.a_generated_concept
{
    void Establish() => Start("concept Code : String\n  generate nanoid\n");

    void Because() => _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");

    [Fact] void should_omit_rules() => _generation.TryGetProperty("rules", out _).ShouldBeFalse();
    [Fact] void should_describe_the_derived_pattern() => _generation.GetProperty("derivedPattern").GetString().ShouldEqual("^[A-Za-z0-9_-]{21}$");
}
