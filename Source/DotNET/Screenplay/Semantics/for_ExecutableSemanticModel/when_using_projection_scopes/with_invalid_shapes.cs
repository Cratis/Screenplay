// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_using_projection_scopes;

// Chronicle keeps SubscribesToAllEvents only on the projection definition (ProjectionDefinitionSyntaxVisitor.cs:28-57, 70-79),
// so only a projection's own level can subscribe to all events, and 'all' always includes children.
public class with_invalid_shapes : given.a_scoped_projection_model
{
    Exception _bothShapes;
    Exception _allBelowTheProjectionLevel;
    Exception _allExcludingChildren;
    Exception _joinOnAnUnknownProperty;

    void Because()
    {
        var flat = new SemanticProjectionTransition(
            EventId("OrderPlaced"),
            new(AffectedInstanceCardinality.One, SemanticExpression.Property(SemanticExpressionRootKind.Event, EventProperty("OrderPlaced", "OrderId"))),
            []);
        _bothShapes = Validate(_orders with { Transitions = [flat] });
        _allBelowTheProjectionLevel = ValidateOrders(OrdersScope with { Children = [Lines with { Scope = Lines.Scope with { Every = new(true, true, []) } }] });
        _allExcludingChildren = ValidateOrders(OrdersScope with { Every = OrdersScope.Every! with { IncludeChildren = false } });
        _joinOnAnUnknownProperty = ValidateOrders(OrdersScope with { Joins = [OrdersScope.Joins.Single() with { On = EventId("OrderPlaced") }] });
    }

    [Fact] void should_reject_flat_transitions_beside_a_scope() => _bothShapes.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_all_below_the_projection_level() => _allBelowTheProjectionLevel.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_all_that_excludes_children() => _allExcludingChildren.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_join_on_a_property_the_level_does_not_have() => _joinOnAnUnknownProperty.ShouldBeOfExactType<InvalidSemanticContract>();
}
