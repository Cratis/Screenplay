// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_concept_with_attribute_reasons : given.a_printer
{
    const string Source =
        """
        concept BankAccount : String @pii @sensitive
          pii reason "Payout account - lawful basis: contract performance"
          sensitive reason "Fraud sensitive - never rendered in full"

        concept InvoiceId : Uuid
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_markers_on_the_header() => _roundtrip.Printed.ShouldContain("concept BankAccount : String @pii @sensitive");
    [Fact] void should_print_the_pii_reason() => _roundtrip.Printed.ShouldContain("pii reason \"Payout account - lawful basis: contract performance\"");
    [Fact] void should_print_the_sensitive_reason() => _roundtrip.Printed.ShouldContain("sensitive reason \"Fraud sensitive - never rendered in full\"");
    [Fact] void should_preserve_the_reasons() => Concept("BankAccount").Attributes.All(_ => _.Reason is not null).ShouldBeTrue();
    [Fact] void should_leave_a_bare_concept_on_one_line() => _roundtrip.Printed.ShouldContain("concept InvoiceId : Uuid");

    ConceptSyntax Concept(string name) => _roundtrip.Reparsed.Value!.Concepts.Single(_ => _.Name == name);
}
