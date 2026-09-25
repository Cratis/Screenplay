// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// A child is upserted by identity under the parent the parent key names (Chronicle ProjectionEventContextExtensions.cs:180-202,
// ProjectionFactory.cs:990-1007); a missing element is created with its identity set; 'remove with' inside children removes one child.
public class child_upserts_by_identity : given.a_scoped_projection_plan
{
    const string Body =
        """
        from OrderShipped
          label = carrier
        children lines identified by lineNumber
          from LineAdded key lineNumber
            parent orderId
            add subtotal by amount
            count quantity
          remove with LineRemoved key lineNumber
            parent orderId
        """;

    string? _parentlessFailure;

    void Establish() => Plan(Body);

    void Because()
    {
        SemanticEvaluator.Establish(
            _compilation.Plan!,
            [],
            [Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10)))],
            out _,
            out _parentlessFailure);
        Project(
            Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(5.5m))),
            Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(2)), ("amount", Number(1))),
            Fact("LineRemoved", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(2))));
    }

    [Fact] void should_pin_the_parentless_child_failure_until_deferral_is_modeled() =>
        _parentlessFailure.ShouldContain("Chronicle defers it until the parent exists");
    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_keep_one_child_per_identity() => Lines.Length.ShouldEqual(1);
    [Fact] void should_update_the_child_in_place() => SemanticValueRules.AreEqual(Member(Lines[0], "OrderLine", "subtotal"), Number(15.5m)).ShouldBeTrue();
    [Fact] void should_count_every_event_for_the_child() => SemanticValueRules.AreEqual(Member(Lines[0], "OrderLine", "quantity"), Number(2)).ShouldBeTrue();
    [Fact] void should_set_the_identity_of_a_created_child() => SemanticValueRules.AreEqual(Member(Lines[0], "OrderLine", "lineNumber"), Number(1)).ShouldBeTrue();

    SemanticValue[] Lines => [.. ((SemanticArrayValue)Value(FirstOrder, "lines")).Values];
}
