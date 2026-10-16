// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_merged_keys : given.a_protected_command
{
    [Theory]
    [InlineData("Recorded")]
    [InlineData("OtherRecorded")]
    [InlineData("Removed")]
    [InlineData("OtherRemoved")]
    void should_merge_handled_event_sets_for_one_runtime_key(string name)
    {
        var world = Append(_world, Fact("OtherRecorded", "read", "second"));
        var decision = _evaluator.Decide(_plan, world, Request("Merge"));
        decision.Dependencies.Entries.Length.ShouldEqual(1);
        decision.Dependencies.Entries.Single().Handled.Count.ShouldEqual(4);
        var latest = Append(world, Fact(name, "read", name.EndsWith("Removed", StringComparison.Ordinal) ? null : "changed"));
        ((SemanticConflict)decision.Commit(latest)).Category.ShouldEqual(SemanticConflictCategory.DecisionStateChanged);
    }
}
