// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle stores parentless children as futures (KeyResolvers.cs:768-784; ResolveFutures.cs),
// but resolving a first-level child's root-parent future in live processing is not yet confirmed.
// The reference evaluator fails closed rather than implementing unverified deferral.
public class a_child_event_before_its_parent : given.a_scoped_projection_plan
{
    const string Body =
        "from OrderShipped\n  label = carrier\nchildren lines identified by lineNumber\n  from LineAdded key lineNumber\n    parent orderId\n    subtotal = amount";

    string? _failureWithoutParent;
    ImmutableArray<SemanticReadModelInstance> _unchanged;

    void Establish() => Plan(Body);

    void Because()
    {
        var child = Fact("LineAdded", "x", ("orderId", Text(FirstOrder)), ("lineNumber", Number(1)), ("amount", Number(10)));
        SemanticEvaluator.Establish(_compilation.Plan!, [], [child], out _unchanged, out _failureWithoutParent);
    }

    [Fact] void should_fail_closed_when_the_parent_does_not_exist() =>
        _failureWithoutParent.ShouldEqual("Projection 'Orders' has no parent for a child event; Chronicle defers it until the parent exists, which the reference evaluator does not model.");
    [Fact] void should_leave_read_models_unchanged() => _unchanged.ShouldBeEmpty();
}
