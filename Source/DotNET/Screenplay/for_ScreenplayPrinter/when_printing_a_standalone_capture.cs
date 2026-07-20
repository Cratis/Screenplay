// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_standalone_capture : given.a_printer
{
    const string Source =
        """
        capture LegacyInvoiceCapture
          source webhook
            path /invoices
          key id
          map
            status = status translate
              "utkast" => draft
              "sendt" => sent
            split fullName by " "
              firstName
              lastName
            summary = `${status} invoice`
          append InvoiceStatusChanged
            when status
              invoiceId = $.id
              status = $.status
          append InvoicePaidFromSent
            when status from "sent" to "paid"
              invoiceId = $.id
          append InvoiceAttentionRequired
            when status or note
              invoiceId = $.id
          append InvoiceFullyApproved
            when approvedByManager and approvedByFinance
              invoiceId = $.id
          append InvoiceFlagged
            when `status == "sent" && overdue == true`
              invoiceId = $.id
          children lineItems identified by lineNumber
            map
              productName = name
            append InvoiceLineItemAdded
              when added
                invoiceId = $.id
                lineNumber = $.lineNumber
            append InvoiceLineItemRemoved
              when removed
                invoiceId = $.id
                lineNumber = $.lineNumber
          nested billingContact
            map
              contactName = name
            append BillingContactUpdated
              when email
                invoiceId = $.id
                email = $.email
        """;

    CompilationResult<CaptureSyntax> _original;
    string _printed;
    CompilationResult<CaptureSyntax> _reparsed;
    string _printedAgain;

    void Because()
    {
        _original = _compiler.CompileCapture(Source);
        _printed = _printer.Print(_original.Value!);
        _reparsed = _compiler.CompileCapture(_printed);
        _printedAgain = _printer.Print(_reparsed.Value!);
    }

    [Fact] void should_reparse_successfully() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_preserve_the_translations() => _reparsed.Value!.Map.OfType<CaptureMapEntrySyntax>().First().Translations.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_split_targets() => _reparsed.Value!.Map.OfType<CaptureSplitSyntax>().Single().Targets.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_value_transition() => When("InvoicePaidFromSent").Kind.ShouldEqual(CaptureWhenKind.ValueTransition);
    [Fact] void should_preserve_the_logical_or() => When("InvoiceAttentionRequired").Kind.ShouldEqual(CaptureWhenKind.LogicalOr);
    [Fact] void should_preserve_the_logical_and() => When("InvoiceFullyApproved").Kind.ShouldEqual(CaptureWhenKind.LogicalAnd);
    [Fact] void should_preserve_the_expression_trigger() => When("InvoiceFlagged").Kind.ShouldEqual(CaptureWhenKind.Expression);
    [Fact] void should_preserve_the_children() => _reparsed.Value!.Children.Single().Appends.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_nested() => _reparsed.Value!.Nested.Single().Property.ShouldEqual("billingContact");

    CaptureWhenSyntax When(string @event) => _reparsed.Value!.Appends.Single(_ => _.Event == @event).When!;
}
