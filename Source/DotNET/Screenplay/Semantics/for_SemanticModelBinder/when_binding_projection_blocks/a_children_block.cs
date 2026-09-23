// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// Chronicle lowers a children body with the same recursive switch as the projection body (ProjectionDefinitionSyntaxVisitor.cs:158-172).
// Inside it the key identifies the child and an absent parent key is the event source identity (ProjectionFactory.cs:990-997).
public class a_children_block : given.a_projection_block_binder
{
    const string Body =
        """
        children lines identified by lineNumber
          from LineAdded key lineNumber
            parent orderId
            lineNumber = lineNumber
            add subtotal by amount
            count quantity
          from OrderShipped
            clear subtotal
          remove with LineRemoved key lineNumber
            parent orderId
        """;

    SemanticProjectionChildren _children;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", Body);
        _children = Scope.Children.Single();
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_collection_property() => _children.Property.ShouldEqual(ReadModelProperty("OrderView", "lines"));
    [Fact] void should_identify_children_by_the_element_property() => _children.IdentifiedBy.ShouldEqual(TypeProperty("OrderLine", "lineNumber"));
    [Fact] void should_key_the_child_on_the_event_property() =>
        ((SemanticProjectionEventProperty)((SemanticProjectionValueKey)_children.Scope.From[0].Key).Value).Path.Single().ShouldEqual(EventProperty("LineAdded", "lineNumber"));
    [Fact] void should_bind_the_declared_parent_key() =>
        ((SemanticProjectionEventProperty)((SemanticProjectionValueKey)_children.Scope.From[0].ParentKey!).Value).Path.Single().ShouldEqual(EventProperty("LineAdded", "orderId"));
    [Fact] void should_default_an_absent_parent_key_to_the_event_source() => _children.Scope.From[1].ParentKey.ShouldEqual(SemanticProjectionKey.EventSourceIdentity);
    [Fact] void should_remove_one_child() => _children.Scope.Removals.Single().EventContract.ShouldEqual(EventId("LineRemoved"));
}
