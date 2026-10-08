// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_expected_outcomes_change : given.a_semantic_comparison
{
    void Because()
    {
        Propose(Source.Replace("\"First\"", "\"Second\"", StringComparison.Ordinal));
        Diff = Read();
    }

    [Fact] void should_report_a_static_expected_outcome_difference() => Items("specifications").Single().GetProperty("changeKind").GetString().ShouldEqual("expected-outcome-changed");
    [Fact] void should_name_the_changed_assertion_member() => Items("specifications").Single().GetProperty("member").GetString().ShouldEqual("thenEvents");
    [Fact] void should_not_execute_specifications() => Diff.GetProperty("comparisonLevel").GetString().ShouldEqual("authoring-structure");
}
