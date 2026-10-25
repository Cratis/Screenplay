// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_capture_has_already_seen_the_key : given.a_settlement
{
    SemanticAccepted _repeat;
    SemanticAccepted _first;
    SemanticAccepted _changed;

    void Establish() => Compile(
        """
        module Shipping
          feature Tracking
            slice Translate Receive
              direction inbound
              event Arrived from "carrier"
                id String
                status String
              event Translated
                id String
                status String
              capture Feed
                source events
                  from Arrived
                key id
                append Translated
                  id = $.id
                  status = $.status
        """);

    void Because()
    {
        var first = Arrived("parcel", "sent");
        _first = (SemanticAccepted)new SemanticReactionSettlement(new SemanticEvaluator()).Settle(_plan, SemanticWorld.Create([first], []), [first], null);

        // The same event again, with the first one already in the world: nothing about the key changed, so the capture has nothing to say.
        var again = Arrived("parcel", "sent");
        _repeat = (SemanticAccepted)new SemanticReactionSettlement(new SemanticEvaluator()).Settle(_plan, SemanticWorld.Create([first, again], []), [again], null);

        var moved = Arrived("parcel", "delivered");
        _changed = (SemanticAccepted)new SemanticReactionSettlement(new SemanticEvaluator()).Settle(_plan, SemanticWorld.Create([first, again, moved], []), [moved], null);
    }

    [Fact] void should_capture_the_first_observation() => _first.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Translated")]);
    [Fact] void should_capture_nothing_for_an_unchanged_repeat() => _repeat.Facts.ShouldBeEmpty();
    [Fact] void should_capture_a_changed_state() => _changed.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Translated")]);

    SemanticFact Arrived(string id, string status)
    {
        var arrived = _plan.Events.Values.Single(contract => contract.Name == "Arrived");
        return new(arrived.Id, SemanticValue.Text(id), [new(arrived.Properties[0].Id, SemanticValue.Text(id)), new(arrived.Properties[1].Id, SemanticValue.Text(status))])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text(id)))
        };
    }
}
