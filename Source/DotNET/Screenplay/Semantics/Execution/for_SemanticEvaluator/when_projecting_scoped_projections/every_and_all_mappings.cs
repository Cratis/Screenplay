// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// 'every' mappings run with each from and join event of their level (Chronicle ProjectionFactory.cs:589-701); 'all' also reaches event
// types no block names, keyed by the event source identity (ProjectionFactory.cs:616-631).
public class every_and_all_mappings : given.a_scoped_projection_plan
{
    ImmutableArray<SemanticReadModelInstance> _every;

    void Establish() => Plan("every\n  count events\nfrom OrderShipped\n  label = carrier");

    void Because()
    {
        Project(Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))), Fact("OrderShipped", FirstOrder, ("carrier", Text("air"))), Fact("CustomerClosed", FirstOrder, ("customerId", Text(Customer))));
        _every = _instances;
        Plan("all\n  count events\nfrom OrderShipped\n  label = carrier");
        Project(Fact("OrderShipped", FirstOrder, ("carrier", Text("post"))), Fact("CustomerClosed", FirstOrder, ("customerId", Text(Customer))), Fact("CustomerClosed", SecondOrder, ("customerId", Text(Customer))));
    }

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_run_every_with_each_from_event_only() =>
        SemanticValueRules.AreEqual(_every.Single().Values.Single(_ => _.TargetProperty == ReadModelProperty("OrderView", "events")).Value, Number(2)).ShouldBeTrue();
    [Fact] void should_run_all_with_unnamed_event_types() => SemanticValueRules.AreEqual(Value(FirstOrder, "events"), Number(2)).ShouldBeTrue();
    [Fact] void should_create_an_instance_for_an_unnamed_event_type() => Instance(SecondOrder).ShouldNotBeNull();
}
