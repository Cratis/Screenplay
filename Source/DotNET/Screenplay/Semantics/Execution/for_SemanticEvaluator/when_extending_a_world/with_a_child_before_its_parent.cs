// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_extending_a_world;

public class with_a_child_before_its_parent : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    const string Order = "00000000-0000-0000-0000-000000000001";

    SemanticEvaluator _evaluator = new();
    SemanticFact _line;
    SemanticFact _order;

    void Establish()
    {
        Compile("""
            concept Id : Uuid
            type Line
              lineNumber Int
              amount Int
            module Orders
              feature Ordering
                slice StateChange CreateOrder
                  command CreateOrder
                    orderId Id identifier
                    produces OrderAdded
                      for orderId
                      orderId = orderId
                  command AddLine
                    orderId Id identifier
                    lineNumber Int
                    amount Int
                    produces LineAdded
                      for orderId
                      orderId = orderId
                      lineNumber = lineNumber
                      amount = amount
                  event OrderAdded
                    orderId Id
                  event LineAdded
                    orderId Id
                    lineNumber Int
                    amount Int
                slice StateView Lookup
                  readmodel OrderView
                    orderId Id
                    lines Line[]
                  query OrderById => OrderView?
                    by orderId Id
                  projection Orders => OrderView
                    from OrderAdded key orderId
                    children lines identified by lineNumber
                      from LineAdded key lineNumber
                        parent orderId
                        amount = amount
            """);
        var idType = _plan.Commands.Values.Single(value => value.Name == "CreateOrder").Properties.Single().Type;
        var added = _plan.Events.Values.Single(value => value.Name == "OrderAdded");
        var line = _plan.Events.Values.Single(value => value.Name == "LineAdded");
        var key = SemanticValue.Text(Order);
        _order = new(added.Id, key, [new(added.Properties.Single().Id, key)]) { Context = new(new(idType, key)) };
        _line = new(line.Id, key, [
            new(line.Properties.Single(property => property.Name == "orderId").Id, key),
            new(line.Properties.Single(property => property.Name == "lineNumber").Id, SemanticValue.Number(1)),
            new(line.Properties.Single(property => property.Name == "amount").Id, SemanticValue.Number(10))])
        { Context = new(new(idType, key)) };
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    void should_equal_the_world_established_from_the_whole_history_including_pending_children(int split)
    {
        SemanticFact[] history = [_line, _order];
        var prefix = _evaluator.EstablishWorld(_plan, [.. history[..split]]).World;
        var extended = (SemanticAccepted)_evaluator.ExtendWorld(_plan, prefix, [.. history[split..]]);
        var expected = _evaluator.EstablishWorld(_plan, [.. history]).World;
        given.world_comparison.AreEqual(extended.World, expected).ShouldBeTrue();
    }

    [Fact]
    void should_keep_the_child_pending_until_its_parent_arrives()
    {
        var pending = (SemanticAccepted)_evaluator.ExtendWorld(_plan, SemanticWorld.Empty, [_line]);
        pending.World.PendingChildren.Length.ShouldEqual(1);
        pending.World.ReadModels.ShouldBeEmpty();
    }

    [Fact]
    void should_resolve_a_pending_child_when_the_parent_arrives_in_the_extension()
    {
        var pending = _evaluator.ExtendWorld(_plan, SemanticWorld.Empty, [_line]).World;
        var resolved = (SemanticAccepted)_evaluator.ExtendWorld(_plan, pending, [_order]);
        resolved.World.PendingChildren.ShouldBeEmpty();
        resolved.World.ReadModels.Length.ShouldEqual(1);
        pending.PendingChildren.Length.ShouldEqual(1);
    }
}
