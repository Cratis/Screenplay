// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class without_produced_facts : given.a_protected_command
{
    [Fact]
    void should_still_check_intervening_dependencies()
    {
        var decision = _evaluator.Decide(_plan, _world, Request("NoFacts"));
        ((SemanticConflict)decision.Commit(Append(_world, Fact("Removed", "read")))).Category.ShouldEqual(SemanticConflictCategory.DecisionStateChanged);
    }

    [Fact]
    void should_accept_without_appending_when_dependencies_stay_unchanged()
    {
        var decision = _evaluator.Decide(_plan, _world, Request("NoFacts"));
        var latest = Append(_world, Fact("OtherRecorded", "read", "other"));
        var result = (SemanticAccepted)decision.Commit(latest);
        result.Facts.ShouldBeEmpty();
        result.World.Facts.SequenceEqual(latest.Facts).ShouldBeTrue();
        result.World.ReadModels.SelectMany(instance => instance.Values).SequenceEqual(latest.ReadModels.SelectMany(instance => instance.Values)).ShouldBeTrue();
    }

    [Fact]
    void should_still_reject_absence()
    {
        ((SemanticRejected)_evaluator.Execute(_plan, SemanticWorld.Empty, Request("NoFacts"))).Details.ShouldEqual("View 'read' does not exist.");
    }
}
