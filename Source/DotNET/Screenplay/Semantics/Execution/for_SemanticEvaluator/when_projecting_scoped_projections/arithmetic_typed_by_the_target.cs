// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Arithmetic is typed by the read-model target: a missing target is seeded with 0 and each operand is converted to the target type
// before the operation - half to even for a whole number (Chronicle PropertyMappers.cs:47-219). A null source sets the target to null
// (PropertyMappers.cs:26-37).
public class arithmetic_typed_by_the_target : given.a_scoped_projection_plan
{
    const string Body =
        """
        from OrderShipped
          label = carrier
          add amount by 2.5
          add total by 2.5
          decrement events
        from LineAdded
          subtract amount by 3.5
        """;

    void Establish() => Plan(Body);

    void Because() => Project(
        Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
        Fact("OrderShipped", FirstOrder, ("carrier", SemanticValue.Null)),
        Fact("LineAdded", FirstOrder, ("orderId", SemanticValue.Null), ("lineNumber", Number(1)), ("amount", Number(1))));

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_round_each_whole_number_operand_before_the_operation() => SemanticValueRules.AreEqual(Value(FirstOrder, "amount"), Number(0)).ShouldBeTrue();
    [Fact] void should_keep_decimal_operands() => SemanticValueRules.AreEqual(Value(FirstOrder, "total"), Number(5)).ShouldBeTrue();
    [Fact] void should_seed_a_missing_target_with_zero() => SemanticValueRules.AreEqual(Value(FirstOrder, "events"), Number(-2)).ShouldBeTrue();
    [Fact] void should_set_the_target_to_null_from_a_null_source() => Value(FirstOrder, "label").ShouldBeOfExactType<SemanticNullValue>();
}
