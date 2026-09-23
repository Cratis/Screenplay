// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_using_projection_scopes;

// Chronicle ProjectionValidator.ValidateDuplicateEvents (ProjectionValidator.cs:92-138): an event type is used once per level
// across from, remove with and remove via join; another level - a child collection or a nested object - starts its own set.
public class with_an_event_used_twice : given.a_scoped_projection_model
{
    Exception _fromAndRemoval;
    Exception _twoJoins;
    Exception _sameEventOnAnotherLevel;

    void Because()
    {
        _fromAndRemoval = ValidateOrders(OrdersScope with { Removals = [new(EventId("OrderPlaced"), SemanticProjectionKey.EventSourceIdentity, null)] });
        _twoJoins = ValidateOrders(OrdersScope with { Joins = [OrdersScope.Joins.Single(), OrdersScope.Joins.Single()] });
        var nested = OrdersScope.Nested.Single();
        _sameEventOnAnotherLevel = ValidateOrders(OrdersScope with
        {
            Nested = [nested with { Scope = nested.Scope with { Removals = [new(EventId("OrderCancelled"), SemanticProjectionKey.EventSourceIdentity, null)] } }]
        });
    }

    [Fact] void should_reject_a_from_and_a_removal_on_the_same_event() => _fromAndRemoval.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_the_same_joined_event_twice() => _twoJoins.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_accept_the_same_event_on_another_level() => _sameEventOnAnotherLevel.ShouldBeNull();
}
