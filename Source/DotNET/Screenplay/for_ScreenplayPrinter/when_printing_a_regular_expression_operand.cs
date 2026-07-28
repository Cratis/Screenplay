// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_regular_expression_operand : given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceNumber String
                validate
                  invoiceNumber matches "^INV-\d{6}$"
        """;

    const string Expected = @"^INV-\d{6}$";

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_an_unknown_escape_verbatim() => Pattern(_roundtrip.Original!).ShouldEqual(Expected);
    [Fact] void should_escape_the_backslash_when_printing() => _roundtrip.Printed.ShouldContain(@"matches ""^INV-\\d{6}$""");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_pattern() => Pattern(_roundtrip.Reparsed).ShouldEqual(Expected);
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    static object? Pattern(CompilationResult<ApplicationSyntax> result) =>
        ((LiteralExpressionSyntax)result.Value!.Modules.Single().Features.Single().Slices.Single()
            .Commands.Single().Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.Single().Value!).Value;
}
