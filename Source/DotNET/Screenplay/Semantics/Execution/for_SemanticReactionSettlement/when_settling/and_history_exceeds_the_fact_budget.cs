// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_history_exceeds_the_fact_budget : given.a_settlement
{
    SemanticAccepted _accepted;

    void Establish()
    {
        Prepare(
            """
            reaction Continue
              when Started
                produces Continued
            """);
        _world = SemanticWorld.Create([.. Enumerable.Repeat(_input, SemanticReactionLoop.MaximumFacts + 1)], []);
    }

    void Because() => _accepted = (SemanticAccepted)_settlement.Settle(_plan, _world, [_input], null);

    [Fact] void should_count_only_the_newly_observed_facts() => _accepted.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Continued")]);
    [Fact] void should_preserve_history_and_append_the_reaction_fact() => _accepted.World.Facts.Length.ShouldEqual(_world.Facts.Length + 1);
}
