// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Set-once reads the occurrence like a plain assignment does, and writes it only into a target that has no value yet (Chronicle#5757).
public class a_set_once_mapping_reading_occurred : given.a_scoped_projection_plan
{
    static readonly DateTimeOffset _first = new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero).AddTicks(1234567);
    static readonly DateTimeOffset _second = new(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    void should_keep_the_first_occurrence_on_the_root()
    {
        Plan("from OrderShipped\n  set once lastSeen = $eventContext.occurred");
        _compilation.Issues.ShouldBeEmpty();
        Project(
            Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))) with { Occurred = _first },
            Fact("OrderShipped", FirstOrder, ("carrier", Text("courier"))) with { Occurred = _second });
        _failure.ShouldBeNull();
        SemanticValueRules.AreEqual(Value(FirstOrder, "lastSeen"), Text("2026-10-02T09:00:00.1234567Z")).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_the_first_occurrence_on_a_child(bool deferred)
    {
        Plan("from OrderShipped\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    count quantity\n    set once lastSeen = $eventContext.occurred");
        _compilation.Issues.ShouldBeEmpty();
        var parent = Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))) with { Occurred = _second };
        var first = Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10))) with { Occurred = _first };
        var second = Fact("LineAdded", SecondOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(20))) with { Occurred = _second };
        Project(deferred ? [first, second, parent] : [parent, first, second]);
        _failure.ShouldBeNull();
        var line = ((SemanticArrayValue)Value(FirstOrder, "lines")).Values.Single();
        SemanticValueRules.AreEqual(Member(line, "OrderLine", "lastSeen"), Text("2026-10-02T09:00:00.1234567Z")).ShouldBeTrue();
    }

    [Fact]
    void should_treat_an_explicit_null_as_no_value_and_assign()
    {
        Plan("from CustomerRegistered\n  label = customerName\nfrom OrderCancelled\n  clear label\nfrom OrderShipped\n  set once label = carrier");
        _compilation.Issues.ShouldBeEmpty();
        Project(
            Fact("CustomerRegistered", FirstOrder, ("customerName", Text("first"))),
            Fact("OrderCancelled", FirstOrder, ("orderId", Text(FirstOrder))),
            Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
            Fact("OrderShipped", FirstOrder, ("carrier", Text("courier"))));
        _failure.ShouldBeNull();
        SemanticValueRules.AreEqual(Value(FirstOrder, "label"), Text("post")).ShouldBeTrue();
    }
}
