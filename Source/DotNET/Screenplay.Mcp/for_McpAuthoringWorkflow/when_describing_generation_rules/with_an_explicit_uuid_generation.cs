// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class with_an_explicit_uuid_generation : given.a_generated_concept
{
    void Establish() => Start("concept Code : Uuid\n  generate uuid\n");

    void Because() => _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");

    [Fact] void should_describe_the_uuid_pattern() => _generation.GetProperty("derivedPattern").GetString().ShouldEqual("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$");
    [Fact] void should_omit_the_unknown_pattern_reason() => _generation.TryGetProperty("derivedPatternUnknownReason", out _).ShouldBeFalse();
}
