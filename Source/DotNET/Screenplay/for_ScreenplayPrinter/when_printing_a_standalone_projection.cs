// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_standalone_projection : given.a_printer
{
    const string Source =
        """
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
        """;

    CompilationResult<ProjectionSyntax> _original;
    string _printed;
    CompilationResult<ProjectionSyntax> _reparsed;
    string _printedAgain;

    void Because()
    {
        _original = _compiler.CompileProjection(Source);
        _printed = _printer.Print(_original.Value!);
        _reparsed = _compiler.CompileProjection(_printed);
        _printedAgain = _printer.Print(_reparsed.Value!);
    }

    [Fact] void should_reparse_successfully() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_preserve_the_read_model() => _reparsed.Value!.ReadModel.ShouldEqual("OrderReadModel");
    [Fact] void should_preserve_the_sequence() => _reparsed.Value!.Sequence.ShouldEqual("orders");
    [Fact] void should_preserve_the_block_count() => _reparsed.Value!.Blocks.Count().ShouldEqual(_original.Value!.Blocks.Count());
}
