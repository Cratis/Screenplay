// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// Chronicle lowers 'nested' to a children definition with no identity, and 'clear with' inside it to a removal that clears the
// object back to null (ProjectionDefinitionSyntaxVisitor.cs:98-100, 174-196). Keys keep addressing the enclosing instance.
public class a_nested_block : given.a_projection_block_binder
{
    const string Body =
        """
        nested shipping
          from OrderShipped
            carrier = carrier
          clear with ShippingCleared
        """;

    SemanticProjectionNested _nested;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", Body);
        _nested = Scope.Nested.Single();
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_nested_property() => _nested.Property.ShouldEqual(ReadModelProperty("OrderView", "shipping"));
    [Fact] void should_map_onto_the_nested_type() => _nested.Scope.From.Single().Mappings.Single(_ => _.Target.Length == 1).Target.Single().ShouldEqual(TypeProperty("Shipping", "carrier"));
    [Fact] void should_clear_with_a_removal() => _nested.Scope.Removals.Single().ShouldEqual(new SemanticProjectionRemoval(EventId("ShippingCleared"), SemanticProjectionKey.EventSourceIdentity, null));
    [Fact] void should_not_create_children() => Scope.Children.ShouldBeEmpty();
}
