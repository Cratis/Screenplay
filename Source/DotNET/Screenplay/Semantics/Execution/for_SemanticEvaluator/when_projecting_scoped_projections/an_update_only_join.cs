// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// A join updates every existing instance whose joined property equals the joined event's source identity and never creates one
// (Chronicle ProjectionEventContextExtensions.cs:89-125, ProjectionFactory.cs:956-973); a from event that maps the joined property
// re-reads the latest joined event (ProjectionFactory.SetupJoinsForFromDefinition, ProjectionFactory.cs:736-784).
public class an_update_only_join : given.a_scoped_projection_plan
{
    const string Body =
        """
        from OrderPlaced key orderId
          no automap
          customerId = customerId
        join customer on customerId
          with CustomerRegistered
            customerName = customerName
        """;

    const string Unknown = "00000000-0000-0000-0000-0000000000c2";

    void Establish() => Plan(Body);

    void Because() => Project(
        Fact("CustomerRegistered", Customer, ("customerName", Text("Ada"))),
        Placed(FirstOrder),
        Placed(SecondOrder),
        Fact("CustomerRegistered", Customer, ("customerName", Text("Ada Lovelace"))),
        Fact("CustomerRegistered", Unknown, ("customerName", Text("Nobody"))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_update_every_matching_instance() =>
        new[] { FirstOrder, SecondOrder }.All(_ => SemanticValueRules.AreEqual(Value(_, "customerName"), Text("Ada Lovelace"))).ShouldBeTrue();
    [Fact] void should_never_create_an_instance() => _instances.Length.ShouldEqual(2);

    SemanticFact Placed(string order) => Fact(
        "OrderPlaced",
        order,
        ("orderId", Text(order)),
        ("customerId", Text(Customer)),
        ("label", Text(order)),
        ("quantity", SemanticValue.Composite([new(TypeProperty("Quantity", "amount"), Number(1)), new(TypeProperty("Quantity", "basis"), Text("box"))])));
}
