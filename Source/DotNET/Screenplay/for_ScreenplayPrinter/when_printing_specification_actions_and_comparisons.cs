// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_specification_actions_and_comparisons : given.a_printer
{
    const string Source =
        """
        specification AppendingAnInvoice
          when append InvoiceRegistered
            for "9c858901-8a57-4791-81fe-4c455b099bc9"
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          then events in any order
          then InvoiceRegistered
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          then readmodel InvoiceSummary exactly
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          then query InvoiceById exactly
            arguments
              invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
            result
              invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        """;

    CompilationResult<SpecificationSyntax> _reparsed;
    string _printed;

    void Because()
    {
        var parsed = _compiler.CompileSpecification(Source);
        _printed = _printer.Print(parsed.Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
    }

    [Fact] void should_reparse() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_round_trip() => _printed.ShouldEqual(_printer.Print(_reparsed.Value!));
    [Fact] void should_retain_appended_event_and_source() => _reparsed.Value!.WhenAppended!.For.ShouldNotBeNull();
    [Fact] void should_leave_command_absent() => _reparsed.Value!.When.ShouldBeNull();
    [Fact] void should_retain_unordered_event_comparison() => _reparsed.Value!.ThenEventsInAnyOrder.ShouldBeTrue();
    [Fact] void should_retain_exact_read_model_comparison() => _reparsed.Value!.ThenReadModels!.Single().Exactly.ShouldBeTrue();
    [Fact] void should_retain_exact_query_comparison() => _reparsed.Value!.ThenQueries.Single().Exactly.ShouldBeTrue();
}
