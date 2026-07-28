// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_text_containing_quotes;

public class and_it_is_a_validation_message : given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceNumber String
                validate
                  invoiceNumber not empty message "Must start with \"INV-\""
        """;

    const string Expected = "Must start with \"INV-\"";

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_unescape_the_message() => Rule(_roundtrip.Original!).Message.ShouldEqual(Expected);
    [Fact] void should_escape_the_quotes_when_printing() => _roundtrip.Printed.ShouldContain("message \"Must start with \\\"INV-\\\"\"");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_message() => Rule(_roundtrip.Reparsed).Message.ShouldEqual(Expected);
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    static ValidationRuleSyntax Rule(CompilationResult<ApplicationSyntax> result) =>
        result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();
}
