// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_module_authorization_changes : given.a_semantic_comparison
{
    void Because()
    {
        const string source = "policy Access\n  require authenticated\nmodule Projects\n  authorize Access\n  feature Registration\n    slice StateChange Register\n      command Register\n";
        CompareSnapshots(source, source.Replace("  authorize Access\n", string.Empty, StringComparison.Ordinal));
    }

    [Fact] void should_report_the_module_authorization_member() => Items("members").Any(item => item.GetProperty("kind").GetString() == "Module" && item.GetProperty("member").GetString() == "authorize").ShouldBeTrue();
    [Fact] void should_not_compare_descendant_subtrees_as_module_members() => Items("members").Any(item => item.GetProperty("kind").GetString() == "Module" && item.GetProperty("member").GetString() == "features").ShouldBeFalse();
    [Fact] void should_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
