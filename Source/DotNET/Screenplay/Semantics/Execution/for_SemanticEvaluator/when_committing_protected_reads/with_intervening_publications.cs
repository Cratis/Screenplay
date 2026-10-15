// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_intervening_publications : given.a_protected_command
{
    const string Publisher = """
            slice Translate Outbound
              direction outbound
              public event Published
                value String
              projection Publisher => Published
                from Saved
                  value = value
        """;

    [Fact]
    void should_conflict_on_a_publication_matching_the_dependency_set()
    {
        Compile(Source.Replace("from Recorded", "from Published", StringComparison.Ordinal) + "\n" + Publisher);
        var world = World(Fact("Published", "read", "original"));
        var decision = _evaluator.Decide(_plan, world, Request());
        var latest = Append(world, Fact("Saved", "read", "changed"));
        latest.Facts.Length.ShouldEqual(3);
        latest.Facts[^1].EventContract.ShouldEqual(_plan.Events.Values.Single(value => value.Name == "Published").Id);
        ((SemanticConflict)decision.Commit(latest)).Category.ShouldEqual(SemanticConflictCategory.DecisionStateChanged);
    }

    [Fact]
    void should_prepare_publication_against_the_commit_world_without_republishing_unchanged_state()
    {
        Compile(Source + "\n" + Publisher);
        var world = World(Fact("Recorded", "read", "original"));
        var decision = _evaluator.Decide(_plan, world, Request());
        var latest = Append(world, Fact("Saved", "write", "original"));
        var result = (SemanticAccepted)decision.Commit(latest);
        result.Facts.Length.ShouldEqual(1);
        result.World.Facts.Length.ShouldEqual(4);
        result.World.Facts[2].EventContract.ShouldEqual(_plan.Events.Values.Single(value => value.Name == "Published").Id);
    }
}
