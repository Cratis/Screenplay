// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// 'remove with' on a projection's own level deletes the instance (Chronicle ProjectionFactory.SetupRemovedWith, ProjectionFactory.cs:277-295).
public class an_instance_removal : given.a_scoped_projection_plan
{
    void Establish() => Plan("from OrderPlaced key orderId\n  no automap\n  label = label\nremove with OrderCancelled key orderId");

    void Because() => Project(
        Fact("OrderPlaced", Customer, ("orderId", Text(FirstOrder)), ("customerId", Text(Customer)), ("label", Text("first")), ("quantity", Quantity())),
        Fact("OrderPlaced", Customer, ("orderId", Text(SecondOrder)), ("customerId", Text(Customer)), ("label", Text("second")), ("quantity", Quantity())),
        Fact("OrderCancelled", Customer, ("orderId", Text(FirstOrder))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_remove_the_keyed_instance() => Instance(FirstOrder).ShouldBeNull();
    [Fact] void should_keep_the_other_instance() => Instance(SecondOrder).ShouldNotBeNull();

    SemanticValue Quantity() => SemanticValue.Composite([new(TypeProperty("Quantity", "amount"), Number(2)), new(TypeProperty("Quantity", "basis"), Text("box"))]);
}
