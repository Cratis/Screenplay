// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_composite_type : given.a_printer
{
    const string Source =
        """
        concept ProductName : String
        concept Money       : Decimal

        type InvoiceLine
          description "A single billed line of an invoice"
          lineNumber  Int
          productName ProductName
          unitPrice   Money
          note        String?
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_type_header() => _roundtrip.Printed.ShouldContain("type InvoiceLine");
    [Fact] void should_print_the_description() => _roundtrip.Printed.ShouldContain("description \"A single billed line of an invoice\"");
    [Fact] void should_print_the_optional_suffix() => _roundtrip.Printed.ShouldContain("note String?");
    [Fact] void should_preserve_the_properties() => Type.Properties.Count().ShouldEqual(4);
    [Fact] void should_preserve_the_description() => Type.Description.ShouldEqual("A single billed line of an invoice");

    TypeSyntax Type => _roundtrip.Reparsed.Value!.Types!.Single();
}
