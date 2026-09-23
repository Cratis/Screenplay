// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// A from event that maps the joined property re-reads the latest joined event, so a customer registered before the order still
// names it (Chronicle ProjectionFactory.SetupJoinsForFromDefinition, ProjectionFactory.cs:736-784; ProjectionEventContextExtensions.cs:89-125).
public class a_join_back_filled_by_a_from_event : given.a_scoped_projection_plan
{
    void Establish() => Plan("from OrderPlaced key orderId\n  no automap\n  customerId = customerId\njoin customer on customerId\n  with CustomerRegistered\n    customerName = customerName");

    void Because() => Project(
        Fact("CustomerRegistered", Customer, ("customerName", Text("Ada"))),
        Fact("CustomerRegistered", Customer, ("customerName", Text("Ada Lovelace"))),
        Fact(
            "OrderPlaced",
            FirstOrder,
            ("orderId", Text(FirstOrder)),
            ("customerId", Text(Customer)),
            ("label", Text("first")),
            ("quantity", SemanticValue.Composite([new(TypeProperty("Quantity", "amount"), Number(1)), new(TypeProperty("Quantity", "basis"), Text("box"))]))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_use_the_latest_joined_event() => SemanticValueRules.AreEqual(Value(FirstOrder, "customerName"), Text("Ada Lovelace")).ShouldBeTrue();
}
