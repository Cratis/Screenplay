// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// With no key the instance is the event source's (Chronicle ProjectionFactory.cs:1009-1017); its key is the read model identifier,
// as a document key is, and collections start empty (ProjectionFactory.cs:329-340).
public class a_from_transition_keyed_on_the_event_source : given.a_scoped_projection_plan
{
    void Establish() => Plan("from OrderShipped\n  label = carrier");

    void Because() => Project(Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))));

    [Fact] void should_compile_a_plan() => _compilation.Success.ShouldBeTrue();
    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_key_the_instance_on_the_event_source() => Instance(FirstOrder).ShouldNotBeNull();
    [Fact] void should_set_the_identifier_to_the_key() => SemanticValueRules.AreEqual(Value(FirstOrder, "orderId"), Text(FirstOrder)).ShouldBeTrue();
    [Fact] void should_map_the_event() => SemanticValueRules.AreEqual(Value(FirstOrder, "label"), Text("post")).ShouldBeTrue();
    [Fact] void should_start_collections_empty() => ((SemanticArrayValue)Value(FirstOrder, "lines")).Values.ShouldBeEmpty();
}
