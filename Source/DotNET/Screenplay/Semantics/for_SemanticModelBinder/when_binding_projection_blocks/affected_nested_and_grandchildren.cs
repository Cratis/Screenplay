// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// ProjectionFactory.SetupNestedSubscriptions/CollectNestedEventTypes subscribes only from/removal/nested;
// KeyResolvers.ForJoin and ChangesetConverter.CreateJoinFilterTarget use the child identity for joins.
public class affected_nested_and_grandchildren : given.a_projection_block_binder
{
    SemanticAffectedProjectionInstance[] _nested;
    SemanticAffectedProjectionInstance[] _grandchildren;
    SemanticAffectedProjectionInstance[] _ignored;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", "nested shipping\n  from OrderShipped\n    carrier = carrier\n  clear with ShippingCleared");
        _nested = [.. Projection.GetAffectedInstances()];
        _result = BindProjection("projection Orders => OrderView", "every\n  count events\nchildren lines identified by lineNumber\n  children parts identified by lineNumber\n    from LineAdded key lineNumber\n      parent lineNumber\n      count quantity\n    join product on lineNumber\n      with ProductUpdated\n        count quantity");
        if (!_result.Success) throw new InvalidOperationException(string.Join("; ", _result.Diagnostics.Select(_ => _.Message)));
        _grandchildren = [.. Projection.GetAffectedInstances()];
        _result = BindProjection("projection Orders => OrderView", "nested shipping\n  join carrier on carrier\n    with OrderShipped\n      note = carrier\n  remove via join on ShippingCleared\n  children lines identified by lineNumber\n    from LineAdded key lineNumber\n      parent orderId\n      count quantity");
        if (!_result.Success) throw new InvalidOperationException(string.Join("; ", _result.Diagnostics.Select(_ => _.Message)));
        _ignored = [.. Projection.GetAffectedInstances()];
    }

    [Fact] void should_report_nested_from_with_a_path() =>
        _nested.Single(_ => _.Block == SemanticAffectedProjectionBlock.From).Path.Single().ShouldEqual(ReadModelProperty("OrderView", "shipping"));
    [Fact] void should_report_removal_clearing_a_nested_object() =>
        _nested.Single(_ => _.Block == SemanticAffectedProjectionBlock.Removal).Path.Single().ShouldEqual(ReadModelProperty("OrderView", "shipping"));
    [Fact] void should_report_a_two_segment_grandchild_from()
    {
        var path = _grandchildren.Single(_ => _.Block == SemanticAffectedProjectionBlock.From).Path;
        path.Length.ShouldEqual(2);
        path[0].ShouldEqual(ReadModelProperty("OrderView", "lines"));
        path[1].ShouldEqual(TypeProperty("OrderLine", "parts"));
    }
    [Fact] void should_report_a_two_segment_grandchild_join_on_its_identity()
    {
        var join = _grandchildren.Single(_ => _.Block == SemanticAffectedProjectionBlock.Join);
        join.Path.Length.ShouldEqual(2);
        join.Property.ShouldEqual(TypeProperty("OrderPart", "lineNumber"));
    }
    [Fact] void should_not_add_a_relationship_for_every() => _grandchildren.Length.ShouldEqual(2);
    [Fact] void should_omit_unsubscribed_nested_joins_children_and_join_removals() => _ignored.ShouldBeEmpty();
}
