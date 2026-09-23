// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// A nested object is created on first touch, merged by later touches and cleared back to null by a removal inside it
// (Chronicle ProjectionDefinitionSyntaxVisitor.cs:174-196, ProjectionFactory.cs:415-470).
public class a_nested_object : given.a_scoped_projection_plan
{
    const string Body =
        """
        from OrderShipped
          label = carrier
        nested shipping
          from OrderShipped
            carrier = carrier
          from LineAdded
            note = "line added"
          clear with ShippingCleared
        """;

    SemanticValue _merged;

    void Establish() => Plan(Body);

    void Because()
    {
        Project(Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))), Fact("LineAdded", FirstOrder, ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(1))));
        _merged = Value(FirstOrder, "shipping");
        Project(Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))), Fact("ShippingCleared", FirstOrder, ("orderId", Text(FirstOrder))));
    }

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_create_the_nested_object_on_first_touch() => SemanticValueRules.AreEqual(Member(_merged, "Shipping", "carrier"), Text("post")).ShouldBeTrue();
    [Fact] void should_merge_later_touches() => SemanticValueRules.AreEqual(Member(_merged, "Shipping", "note"), Text("line added")).ShouldBeTrue();
    [Fact] void should_clear_the_nested_object_to_null() => Value(FirstOrder, "shipping").ShouldBeOfExactType<SemanticNullValue>();
}
