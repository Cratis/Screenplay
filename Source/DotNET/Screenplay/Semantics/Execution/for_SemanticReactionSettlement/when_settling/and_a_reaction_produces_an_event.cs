// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_reaction_produces_an_event : given.a_settlement
{
    SemanticAccepted _accepted;
    int _observations;

    void Establish() => Prepare(
        """
        reaction Continue
          when Started
            produces Continued
        """);

    void Because()
    {
        var appended = new[] { _input }.Select(fact =>
        {
            _observations++;
            return fact;
        });
        _result = _settlement.Settle(_plan, _world, appended, _occurred);
        _accepted = (SemanticAccepted)_result;
    }

    [Fact] void should_accept_the_settled_cascade() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_return_only_the_reaction_fact() => _accepted.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Continued")]);
    [Fact] void should_keep_the_input_and_add_the_reaction_fact_to_the_world() => _accepted.World.Facts.AsEnumerable().ShouldEqual([_input, _accepted.Facts[0]]);
    [Fact] void should_keep_the_triggering_event_source() => _accepted.Facts[0].Destination.ShouldEqual(_input.Destination);
    [Fact] void should_propagate_the_occurrence_time() => _accepted.Facts[0].Occurred.ShouldEqual(_occurred);
    [Fact] void should_not_change_the_input_world() => _world.Facts.AsEnumerable().ShouldEqual([_input]);
    [Fact] void should_enumerate_appended_facts_only_once() => _observations.ShouldEqual(1);
    [Fact] void should_return_no_queries() => _accepted.Queries.ShouldBeEmpty();
    [Fact] void should_return_no_response() => _accepted.Response.ShouldBeNull();
}
