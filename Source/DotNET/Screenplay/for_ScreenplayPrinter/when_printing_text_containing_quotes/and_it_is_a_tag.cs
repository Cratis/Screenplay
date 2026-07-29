// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_text_containing_quotes;

public class and_it_is_a_tag : given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              event InvoiceRegistered
                tag "release \"2024.1\""
                invoiceId Uuid
        """;

    const string Expected = "release \"2024.1\"";

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_unescape_the_tag() => TagValue(_roundtrip.Original!).ShouldEqual(Expected);
    [Fact] void should_escape_the_quotes_when_printing() => _roundtrip.Printed.ShouldContain("tag \"release \\\"2024.1\\\"\"");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_tag() => TagValue(_roundtrip.Reparsed).ShouldEqual(Expected);
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    static object? TagValue(CompilationResult<ApplicationSyntax> result) =>
        ((LiteralExpressionSyntax)result.Value!.Modules.Single().Features.Single().Slices.Single()
            .Events.Single().Tags!.Single().Value).Value;
}
