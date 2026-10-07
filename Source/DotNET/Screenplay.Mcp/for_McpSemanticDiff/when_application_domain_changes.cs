// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_application_domain_changes : given.a_semantic_comparison
{
    void Because()
    {
        const string source = "domain Before\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n";
        CompareSnapshots(source, source.Replace("domain Before", "domain After", StringComparison.Ordinal));
    }

    [Fact] void should_compare_the_application_own_domain_member() => Items("members").Any(item => item.GetProperty("kind").GetString() == "Application" && item.GetProperty("member").GetString() == "domain").ShouldBeTrue();
    [Fact] void should_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
