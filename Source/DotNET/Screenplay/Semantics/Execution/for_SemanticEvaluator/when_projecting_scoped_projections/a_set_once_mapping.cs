// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Set-once writes only into a target that has no value yet - missing or null - and leaves a held value as it is (Chronicle#5757).
public class a_set_once_mapping : given.a_scoped_projection_plan
{
    const string Body =
        """
        from CustomerRegistered
          set once label = customerName
        from OrderShipped
          set once label = carrier
        """;

    void Establish() => Plan(Body);

    void Because()
    {
        Project(
            Fact("CustomerRegistered", FirstOrder, ("customerName", Text("first"))),
            Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))),
            Fact("OrderShipped", SecondOrder, ("carrier", Text("courier"))),
            Fact("CustomerRegistered", SecondOrder, ("customerName", Text("late"))));
    }

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_keep_the_first_value() => SemanticValueRules.AreEqual(Value(FirstOrder, "label"), Text("first")).ShouldBeTrue();
    [Fact] void should_write_into_an_empty_target_whichever_event_comes_first() => SemanticValueRules.AreEqual(Value(SecondOrder, "label"), Text("courier")).ShouldBeTrue();
    [Fact] void should_bind_the_model_to_esm_v42() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V42);
}
