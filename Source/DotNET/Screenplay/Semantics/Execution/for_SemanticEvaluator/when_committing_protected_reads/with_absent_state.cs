// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_absent_state : given.a_protected_command
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_reject_absence_in_empty_and_nonempty_logs(bool history)
    {
        var world = history ? _world : SemanticWorld.Empty;
        var result = (SemanticRejected)_evaluator.Execute(_plan, world, Request(lookup: "missing"));
        result.Category.ShouldEqual(SemanticRejectionCategory.Validation);
        result.Details.ShouldEqual("View 'missing' does not exist.");
        result.World.ShouldBeSame(world);
    }

    [Fact]
    void should_drop_unused_dependencies_without_committing_a_later_recreation()
    {
        var decision = _evaluator.Decide(_plan, SemanticWorld.Empty, Request());
        decision.Dependencies.Entries.ShouldBeEmpty();
        var latest = Append(SemanticWorld.Empty, Fact("Recorded", "read", "now present"));
        var result = (SemanticRejected)decision.Commit(latest);
        result.Details.ShouldEqual("View 'read' does not exist.");
        result.World.ShouldBeSame(latest);
    }

    [Fact]
    void should_reject_after_removal_without_confusing_another_keys_recreation()
    {
        var world = World(Fact("Recorded", "read", "old"), Fact("Removed", "read"), Fact("Recorded", "other", "new"));
        ((SemanticRejected)_evaluator.Execute(_plan, world, Request())).Details.ShouldEqual("View 'read' does not exist.");
    }

    [Fact]
    void should_read_the_recreated_instance()
    {
        var world = World(Fact("Recorded", "read", "old"), Fact("Removed", "read"), Fact("Recorded", "read", "new"));
        ((SemanticAccepted)_evaluator.Execute(_plan, world, Request())).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("new"));
    }
}
