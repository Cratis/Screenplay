// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// Chronicle lowers 'from A, B' to one from-definition per event sharing the block's mappings
// (ProjectionDefinitionSyntaxVisitor.ProcessFrom, ProjectionDefinitionSyntaxVisitor.cs:109-121).
public class a_from_block_naming_several_events : given.a_projection_block_binder
{
    void Because() => _result = BindProjection("projection Orders => OrderView", "from LineRemoved, OrderCancelled\n  key orderId\n  events = 1");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_flat_shape_it_can_express() => Projection.Scope.ShouldBeNull();
    [Fact] void should_create_one_transition_per_event() => Projection.Transitions.Select(_ => _.EventContract).ShouldContainOnly([EventId("LineRemoved"), EventId("OrderCancelled")]);
    [Fact] void should_share_the_mappings() => Projection.Transitions.All(_ => _.Mappings.Any(mapping => mapping.TargetProperty == ReadModelProperty("OrderView", "events"))).ShouldBeTrue();
}
