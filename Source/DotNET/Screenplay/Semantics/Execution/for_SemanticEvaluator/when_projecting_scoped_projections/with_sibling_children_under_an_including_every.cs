// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle subscribes an ancestor's include-children every once per distinct child-only event type
// (ChildEveryEventMappings.cs:22-27,55-71). The ancestor's own Event stream fires once per event
// (HandleEvent.cs:30-34, Projection.cs:78-79,194-195), even when sibling children both handle it.
public class with_sibling_children_under_an_including_every : given.a_self_contained_projection_plan
{
    const string Order = "00000000-0000-0000-0000-000000000001";
    const string Source =
        """
        concept Id : Uuid
        type Line
          lineNumber Int
          amount Int
        module Orders
          feature Ordering
            slice StateView Lookup
              event OrderAdded
                orderId Id
              event LineAdded
                orderId Id
                lineNumber Int
                amount Int
              readmodel OrderView
                orderId Id
                events Int
                lines Line[]
                credits Line[]
              query OrderById => OrderView?
                by orderId Id
              projection Orders => OrderView
                from OrderAdded key orderId
                every include children
                  count events
                children lines identified by lineNumber
                  from LineAdded key lineNumber
                    parent orderId
                    amount = amount
                children credits identified by lineNumber
                  from LineAdded key lineNumber
                    parent orderId
                    amount = amount
        """;

    void Establish() => Plan(Source);

    void Because() => Project(
        Fact("OrderAdded", Order, ("orderId", Text(Order))),
        Fact("LineAdded", Order, ("orderId", Text(Order)), ("lineNumber", Number(1)), ("amount", Number(10))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_add_the_line_to_both_collections() => new[] { "lines", "credits" }.ShouldEachConformTo(property => ((SemanticArrayValue)Value("OrderView", Order, property)).Values.Length == 1);
    [Fact] void should_count_the_child_event_once_on_the_ancestor() => ((SemanticNumberValue)Value("OrderView", Order, "events")).Value.ShouldEqual(2m);
}
