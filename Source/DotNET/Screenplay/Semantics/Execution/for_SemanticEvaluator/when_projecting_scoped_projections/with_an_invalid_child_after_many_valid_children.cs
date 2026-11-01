// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class with_an_invalid_child_after_many_valid_children : given.a_scoped_projection_plan
{
    SemanticFact[] _validFacts;
    SemanticValue[] _lines;
    string? _validFailure;

    void Establish()
    {
        _result = Bind(
            """
            concept OrderId : Uuid
            type OrderLine
              lineNumber Int
              quantity Int
            module Orders
              feature Ordering
                slice StateChange Events
                  event OrderShipped
                    carrier String
                  event LineAdded
                    orderId OrderId
                    lineNumber Int
                    amount Int
                slice StateView Lookup
                  readmodel OrderView
                    orderId OrderId
                    label String?
                    lines OrderLine[]
                  query OrderById => OrderView?
                    by orderId OrderId
                  projection Orders => OrderView
                    from OrderShipped
                      label = carrier
                    children lines identified by lineNumber
                      from LineAdded key lineNumber
                        parent orderId
                        quantity = amount
            """);
        Errors.ShouldBeEmpty();
        _compilation = SemanticExecutionPlan.Compile(_result.Value!.Model);
        _validFacts = [
            Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
            .. Enumerable.Range(1, 50).Select(line => Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(line)), ("amount", Number(line))))];
        Project(_validFacts);
        _validFailure = _failure;
        _lines = [.. ((SemanticArrayValue)Value(FirstOrder, "lines")).Values];
    }

    void Because() => Project([
        .. _validFacts,
        Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(1.5m)))]);

    [Fact] void should_accept_the_valid_children() => _validFailure.ShouldBeNull();
    [Fact] void should_preserve_child_insertion_order() => _lines.Select(line => ((SemanticNumberValue)Member(line, "OrderLine", "lineNumber")).Value).ShouldEqual(Enumerable.Range(1, 50).Select(line => (decimal)line));
    [Fact] void should_reject_the_changed_child_with_the_original_message() => _failure.ShouldEqual("A property 'quantity' of element of read-model property 'lines' is incompatible with its target type.");
}
