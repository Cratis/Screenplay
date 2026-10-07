// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_reaction_schedule_changes : given.a_reactive_comparison
{
    void Because() => CompareSnapshots(ReactiveSource, ReactiveSource.Replace("every 15 minutes", "every 30 minutes", StringComparison.Ordinal));

    [Fact] void should_report_the_reaction_schedule_member() => Items("members").Any(item => item.GetProperty("kind").GetString() == "Reaction").ShouldBeTrue();
    [Fact] void should_not_silently_skip_an_assigned_reaction() => Section("members").GetProperty("complete").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
