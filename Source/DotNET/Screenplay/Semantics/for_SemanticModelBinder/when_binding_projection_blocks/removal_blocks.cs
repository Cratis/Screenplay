// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// 'remove with' and 'clear with' lower to the same removal at every level, and 'remove via join' to a join removal with no
// parent key (ProjectionDefinitionSyntaxVisitor.cs:89-100). A join removal key is matched against the level's identity
// (ProjectionFactory.cs:874, KeyResolvers.ForJoin).
public class removal_blocks : given.a_projection_block_binder
{
    const string Body =
        """
        from OrderPlaced key orderId
          label = label
        remove with OrderCancelled key orderId
        clear with ShippingCleared
        remove via join on CustomerClosed
        """;

    void Because() => _result = BindProjection("projection Orders => OrderView", Body);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_remove_with_the_declared_key() =>
        ((SemanticProjectionEventProperty)((SemanticProjectionValueKey)Scope.Removals[0].Key).Value).Path.Single().ShouldEqual(EventProperty("OrderCancelled", "orderId"));
    [Fact] void should_clear_with_the_default_key() => Scope.Removals[1].Key.ShouldEqual(SemanticProjectionKey.EventSourceIdentity);
    [Fact] void should_remove_via_join() => Scope.JoinRemovals.Single().EventContract.ShouldEqual(EventId("CustomerClosed"));
}
