// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.when_walking_a_sub_language_fragment;

public class and_it_is_a_projection : Specification
{
    const string Source =
        """
        # A projection using the full feature set of the sub-language
        projection Order => OrderReadModel
          sequence orders
          every
            lastUpdated = $eventContext.occurred
            exclude children

          from OrderPlaced key orderId
            orderNumber = orderNumber
            placedBy = $causedBy.name
            placedByUser = $causedBy.userName
            reference = `order-${orderNumber}`
            total = total
            status = "Pending"

          from OrderShipped
            status = "Shipped"

          join customer on customerId
            with CustomerCreated
              customerName = name

          children items identified by lineNumber
            from LineItemAdded key lineNumber
              parent orderId
              add total by amount
              count occurrences
            remove with LineItemRemoved key lineNumber
              parent orderId

          nested shipping
            from OrderShipped
              carrier = carrier
            clear with ShippingCleared

          remove with OrderCancelled
        """;

    given.a_counting_walker _walker;
    Projections.ProjectionSyntax _projection;

    void Establish()
    {
        _walker = new();
        _projection = new ScreenplayCompiler().CompileProjection(Source).Value!;
    }

    void Because() => _walker.VisitProjection(_projection);

    [Fact] void should_reach_every_node_the_fragment_holds() => _walker.Nodes.Count.ShouldEqual(given.SyntaxNodes.Under(_projection).Count);
    [Fact] void should_reach_every_top_level_block() => _projection.Blocks.All(_walker.Nodes.Contains).ShouldBeTrue();
    [Fact] void should_descend_into_the_blocks_a_block_nests() => _walker.Nodes.OfType<Projections.ClearWithSyntax>().Count().ShouldEqual(1);
}
