// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class with_a_deterministic_pattern : given.a_generated_concept
{
    JsonElement _rule;

    void Establish() => Start("concept Code : String\n  generate pattern \"ORD-#{7}\"\n  validate\n    min 11\n");

    void Because()
    {
        _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");
        _rule = _generation.GetProperty("rules")[0];
    }

    [Fact] void should_report_the_compiler_proof() => _rule.GetProperty("status").GetString().ShouldEqual("proven");
    [Fact] void should_derive_the_pattern_shape() => _generation.GetProperty("derivedPattern").ValueKind.ShouldEqual(JsonValueKind.String);
    [Fact] void should_match_a_generated_shape() => Regex.IsMatch("ORD-1234567", _generation.GetProperty("derivedPattern").GetString()!).ShouldBeTrue();
    [Fact] void should_exclude_a_different_shape() => Regex.IsMatch("ORD-not-digits", _generation.GetProperty("derivedPattern").GetString()!).ShouldBeFalse();
}
