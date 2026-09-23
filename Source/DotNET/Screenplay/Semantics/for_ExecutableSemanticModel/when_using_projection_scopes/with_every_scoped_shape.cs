// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_using_projection_scopes;

// The mirror positive case for every rejection in this folder: the golden #211 slice uses every scoped shape and validates.
public class with_every_scoped_shape : given.a_scoped_projection_model
{
    Exception _exception;

    void Because() => _exception = Validate(_orders);

    [Fact] void should_validate() => _exception.ShouldBeNull();
    [Fact] void should_carry_no_flat_transitions() => _orders.Transitions.ShouldBeEmpty();
    [Fact] void should_carry_one_transition_per_event() => OrdersScope.From.Select(_ => _.EventContract).ShouldContainOnly([EventId("OrderPlaced"), EventId("OrderReopened")]);
    [Fact] void should_carry_the_children() => Lines.IdentifiedBy.ShouldEqual(TypeProperty("OrderLine", "LineNumber"));
    [Fact] void should_carry_the_nested_object() => OrdersScope.Nested.Single().Property.ShouldEqual(ReadModelProperty("Shipping"));
    [Fact] void should_subscribe_the_projection_level_to_all_events() => OrdersScope.Every!.SubscribesToAllEvents.ShouldBeTrue();
}
