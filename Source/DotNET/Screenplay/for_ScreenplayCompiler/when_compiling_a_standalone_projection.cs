// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_standalone_projection : given.a_compiler
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

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_name() => _result.Value!.Name.ShouldEqual("Order");
    [Fact] void should_have_the_read_model() => _result.Value!.ReadModel.ShouldEqual("OrderReadModel");
    [Fact] void should_have_the_sequence() => _result.Value!.Sequence.ShouldEqual("orders");
    [Fact] void should_parse_the_every_block() => _result.Value!.Blocks.OfType<EverySyntax>().Single().IncludeChildren.ShouldBeFalse();
    [Fact] void should_parse_the_event_context_expression() => _result.Value!.Blocks.OfType<EverySyntax>().Single().Mappings.OfType<SetMappingSyntax>().Single().Source.ShouldBeOfExactType<EventContextExpressionSyntax>();
    [Fact] void should_parse_the_inline_key() => FirstFrom.Events.Single().Key.ShouldBeOfExactType<PathExpressionSyntax>();
    [Fact] void should_parse_the_caused_by_expression() => ((CausedByExpressionSyntax)Mapping(FirstFrom, "placedBy").Source).Property.ShouldEqual("name");
    [Fact] void should_parse_the_template_expression() => Mapping(FirstFrom, "reference").Source.ShouldBeOfExactType<TemplateExpressionSyntax>();
    [Fact] void should_parse_the_string_literal() => ((LiteralExpressionSyntax)Mapping(FirstFrom, "status").Source).Value.ShouldEqual("Pending");
    [Fact] void should_parse_the_join() => _result.Value!.Blocks.OfType<JoinSyntax>().Single().Events.Single().Event.ShouldEqual("CustomerCreated");
    [Fact] void should_parse_the_children_identity() => Children.IdentifiedBy.ShouldBeOfExactType<PathExpressionSyntax>();
    [Fact] void should_parse_the_child_parent_key() => Children.Blocks.OfType<FromSyntax>().Single().ParentKey.ShouldNotBeNull();
    [Fact] void should_parse_the_add_mapping() => Children.Blocks.OfType<FromSyntax>().Single().Mappings.OfType<AddMappingSyntax>().Single().Property.ShouldEqual("total");
    [Fact] void should_parse_the_count_mapping() => Children.Blocks.OfType<FromSyntax>().Single().Mappings.OfType<CountMappingSyntax>().Single().Property.ShouldEqual("occurrences");
    [Fact] void should_parse_the_child_removal() => Children.Blocks.OfType<RemoveWithSyntax>().Single().Event.ShouldEqual("LineItemRemoved");
    [Fact] void should_parse_the_nested_block() => Nested.Blocks.OfType<FromSyntax>().Count().ShouldEqual(1);
    [Fact] void should_parse_the_clear_with() => Nested.Blocks.OfType<ClearWithSyntax>().Single().Event.ShouldEqual("ShippingCleared");
    [Fact] void should_parse_the_projection_level_removal() => _result.Value!.Blocks.OfType<RemoveWithSyntax>().Single().Event.ShouldEqual("OrderCancelled");

    FromSyntax FirstFrom => _result.Value!.Blocks.OfType<FromSyntax>().First();

    ChildrenSyntax Children => _result.Value!.Blocks.OfType<ChildrenSyntax>().Single();

    NestedSyntax Nested => _result.Value!.Blocks.OfType<NestedSyntax>().Single();

    static SetMappingSyntax Mapping(FromSyntax from, string property) =>
        from.Mappings.OfType<SetMappingSyntax>().Single(_ => _.Property == property);
}
