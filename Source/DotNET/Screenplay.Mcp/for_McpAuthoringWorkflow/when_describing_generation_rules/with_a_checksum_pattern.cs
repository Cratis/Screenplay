// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules;

public class with_a_checksum_pattern : given.a_generated_concept
{
    JsonElement _rule;

    void Establish() => Start("concept Code : String\n  generate pattern \"ORD-#{7}\" checksum luhn\n  validate\n    checksum luhn\n");

    void Because()
    {
        _generation = Result("declaration-details", new { address = "Code", kind = "Concept", view = "summary" }).GetProperty("details").GetProperty("generation");
        _rule = _generation.GetProperty("rules")[0];
    }

    [Fact] void should_describe_the_checksum_validation() => _rule.GetProperty("rule").GetString().ShouldEqual("checksum");
    [Fact] void should_report_the_compiler_proof() => _rule.GetProperty("status").GetString().ShouldEqual("proven");
    [Fact] void should_include_the_checksum_digit_in_the_derived_shape() => Regex.IsMatch("ORD-12345674", _generation.GetProperty("derivedPattern").GetString()!).ShouldBeTrue();
    [Fact] void should_not_claim_a_proof_diagnostic() => _rule.TryGetProperty("diagnosticCode", out _).ShouldBeFalse();
}
