// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// A single projection exercises the five scoped block shapes and the wildcard all subscription.
public class affected_instances : given.a_projection_block_binder
{
    const string Body =
        """
        from OrderPlaced key orderId
          label = label
        all
          count events
        join customer on customerId
          with CustomerRegistered
            customerName = customerName
        remove with OrderCancelled key orderId
        children lines identified by lineNumber
          from LineAdded key lineNumber
            parent orderId
            count quantity
          join product on productId
            with ProductUpdated
              count quantity
          remove via join on LineRemoved key lineNumber
        """;

    SemanticAffectedProjectionInstance[] _affected;
    SemanticAffectedProjectionInstance _rootJoinRemoval;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", Body);
        if (_result.Success)
        {
            _affected = [.. Projection.GetAffectedInstances()];
        }

        var rootRemoval = BindProjection("projection Unverified => OrderView", "remove via join on CustomerClosed");
        _rootJoinRemoval = rootRemoval.Value!.Model.Application.Modules.Single().Features.Single()
            .Slices.SelectMany(_ => _.Projections).Single().GetAffectedInstances().Single();
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_from_as_one_by_key() =>
        _affected.Single(_ => _.Block == SemanticAffectedProjectionBlock.From && _.Path.IsEmpty).Match.ShouldEqual(SemanticAffectedProjectionMatch.OneByKey);
    [Fact] void should_bind_all_as_one_by_event_source_with_an_open_subscription()
    {
        var all = _affected.Single(_ => _.Block == SemanticAffectedProjectionBlock.All);
        all.Match.ShouldEqual(SemanticAffectedProjectionMatch.OneByEventSource);
        all.EventContract.ShouldBeNull();
    }
    [Fact] void should_bind_a_root_join_as_many_by_property_and_event_source()
    {
        var join = _affected.Single(_ => _.Block == SemanticAffectedProjectionBlock.Join && _.Path.IsEmpty);
        join.Match.ShouldEqual(SemanticAffectedProjectionMatch.ManyByPropertyAndEventSource);
        join.Property.ShouldEqual(ReadModelProperty("OrderView", "customerId"));
        join.EventContract.ShouldEqual(EventId("CustomerRegistered"));
    }
    [Fact] void should_bind_removal_as_one_by_key() =>
        _affected.Single(_ => _.Block == SemanticAffectedProjectionBlock.Removal).Match.ShouldEqual(SemanticAffectedProjectionMatch.OneByKey);
    [Fact] void should_bind_a_child_join_as_many_across_parents()
    {
        var join = _affected.Single(_ => _.Block == SemanticAffectedProjectionBlock.Join && !_.Path.IsEmpty);
        join.Match.ShouldEqual(SemanticAffectedProjectionMatch.ManyChildrenByKey);
        join.Path.Single().ShouldEqual(ReadModelProperty("OrderView", "lines"));
        join.Property.ShouldEqual(TypeProperty("OrderLine", "lineNumber"));
        join.Property.ShouldNotEqual(TypeProperty("OrderLine", "productId"));
        join.EventContract.ShouldEqual(EventId("ProductUpdated"));
    }
    [Fact] void should_bind_remove_via_join_as_many_across_parents()
    {
        var removal = _affected.Single(_ => _.Block == SemanticAffectedProjectionBlock.JoinRemoval);
        removal.Match.ShouldEqual(SemanticAffectedProjectionMatch.ManyChildrenByKey);
        removal.Property.ShouldEqual(TypeProperty("OrderLine", "lineNumber"));
        removal.EventContract.ShouldEqual(EventId("LineRemoved"));
    }
    [Fact] void should_report_unverified_root_join_removal_without_throwing() =>
        _rootJoinRemoval.Match.ShouldEqual(SemanticAffectedProjectionMatch.Unverified);
    [Fact] void should_not_warn_about_unexpressed_transition_cardinalities() =>
        _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.DeprecatedProjectionTransitionCardinality).ShouldBeFalse();
}
