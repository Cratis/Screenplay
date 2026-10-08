// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_slice_kind_changes : given.a_semantic_comparison
{
    void Because()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
        CompareSnapshots(source, source.Replace("slice StateChange", "slice StateView", StringComparison.Ordinal));
    }

    [Fact] void should_report_the_slice_own_member_change() => Items("members").Any(item => item.GetProperty("kind").GetString() == "Slice" && item.GetProperty("member").GetString() == "type").ShouldBeTrue();
    [Fact] void should_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
