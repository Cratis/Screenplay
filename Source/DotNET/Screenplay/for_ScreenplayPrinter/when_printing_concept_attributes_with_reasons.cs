// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_concept_attributes_with_reasons : given.a_printer
{
    const string Source =
        """
        concept BankAccountNumber : String @pii("Partner payout account - lawful basis: \"contract performance\".") @sensitive
        concept EmailAddress : String @pii
        concept Classified : String @pii("first") @sensitive("second")
        concept Awkward : String @pii("a close paren ) an at @sensitive and a backslash \\ inside")
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_escape_the_quotes_in_the_reason() => _roundtrip.Printed.ShouldContain("@pii(\"Partner payout account - lawful basis: \\\"contract performance\\\".\")");
    [Fact] void should_print_a_bare_attribute_without_parentheses() => _roundtrip.Printed.ShouldContain("concept EmailAddress : String @pii\n");
    [Fact] void should_preserve_the_reason() => Attribute("BankAccountNumber", "pii").Value.ShouldEqual("Partner payout account - lawful basis: \"contract performance\".");
    [Fact] void should_preserve_the_argumentless_attribute() => Attribute("BankAccountNumber", "sensitive").Value.ShouldBeNull();
    [Fact] void should_preserve_the_first_of_two_arguments() => Attribute("Classified", "pii").Value.ShouldEqual("first");
    [Fact] void should_preserve_the_second_of_two_arguments() => Attribute("Classified", "sensitive").Value.ShouldEqual("second");
    [Fact] void should_preserve_a_reason_holding_grammar_characters() => Attribute("Awkward", "pii").Value.ShouldEqual(@"a close paren ) an at @sensitive and a backslash \ inside");

    ConceptAttributeSyntax Attribute(string concept, string name) =>
        _roundtrip.Reparsed.Value!.Concepts.Single(_ => _.Name == concept).Attributes.Single(_ => _.Name == name);
}
