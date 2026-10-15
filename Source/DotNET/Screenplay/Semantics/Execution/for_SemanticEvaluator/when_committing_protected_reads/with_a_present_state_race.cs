// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_a_present_state_race : given.a_protected_command
{
    [Theory]
    [InlineData("Recorded", "read", true)]
    [InlineData("Removed", "read", true)]
    [InlineData("Recorded", "write", false)]
    [InlineData("OtherRecorded", "read", false)]
    void should_conflict_only_on_a_handled_event_at_the_read_key(string name, string key, bool conflict)
    {
        var decision = _evaluator.Decide(_plan, _world, Request());
        var latest = Append(_world, Fact(name, key, name == "Removed" ? null : "changed"));
        var result = decision.Commit(latest);
        decision.Boundary.ShouldEqual(1);
        if (conflict)
        {
            ((SemanticConflict)result).Category.ShouldEqual(SemanticConflictCategory.DecisionStateChanged);
            result.World.ShouldBeSame(latest);
            result.World.Facts.Length.ShouldEqual(2);
        }
        else
        {
            ((SemanticAccepted)result).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("original"));
            result.World.Facts.Length.ShouldEqual(3);
        }
    }
}
