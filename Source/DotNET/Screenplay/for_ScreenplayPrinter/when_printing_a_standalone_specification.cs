// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_standalone_specification : given.a_printer
{
    const string Source =
        """
        specification RegisteringADraftInvoice
          given CustomerRegistered
            customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            name = "Acme Corp"
          when RegisterInvoice
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
            customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          then InvoiceRegistered
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
            customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          then ProFormaInvoiceIssued
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          then error "An invoice must have at least one line"
        """;

    CompilationResult<SpecificationSyntax> _original;
    string _printed;
    CompilationResult<SpecificationSyntax> _reparsed;
    string _printedAgain;

    void Because()
    {
        _original = _compiler.CompileSpecification(Source);
        _printed = _printer.Print(_original.Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
        _printedAgain = _printer.Print(_reparsed.Value!);
    }

    [Fact] void should_reparse_successfully() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_preserve_the_given_events() => _reparsed.Value!.Given.Count().ShouldEqual(_original.Value!.Given.Count());
    [Fact] void should_preserve_the_when() => _reparsed.Value!.When!.CommandType.ShouldEqual("RegisterInvoice");
    [Fact] void should_preserve_the_then_events() => _reparsed.Value!.ThenEvents.Count().ShouldEqual(_original.Value!.ThenEvents.Count());
    [Fact] void should_preserve_the_then_error() => _reparsed.Value!.ThenErrors.Single().Name.ShouldEqual("An invoice must have at least one line");
}
