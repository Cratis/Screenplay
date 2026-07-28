// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_numeric_literals;

public class and_the_culture_uses_a_comma_decimal_separator : given.a_printer
{
    given.a_printer.RoundTripResult _roundtrip;
    string _separator;

    void Because()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("nb-NO");
        try
        {
            _separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            _roundtrip = RoundTrip(Application());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact] void should_have_printed_under_a_comma_decimal_culture() => _separator.ShouldEqual(",");
    [Fact] void should_print_a_decimal_with_an_invariant_separator() => _roundtrip.Printed.ShouldContain("max 5.5");
    [Fact] void should_print_a_float_with_an_invariant_separator() => _roundtrip.Printed.ShouldContain("max 2.25");
    [Fact] void should_print_an_int_verbatim() => _roundtrip.Printed.ShouldContain("max 7");
    [Fact] void should_print_a_long_verbatim() => _roundtrip.Printed.ShouldContain("max 9");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    static ApplicationSyntax Application() =>
        new(
            [],
            [
                Concept("Amount", "Decimal", 5.5m),
                Concept("Ratio", "Decimal", 2.25f),
                Concept("Count", "Int", 7),
                Concept("Total", "Int", 9L)
            ],
            [],
            [],
            SourceLocation.Start);

    static ConceptSyntax Concept(string name, string type, object value) =>
        new(
            name,
            type,
            [],
            [],
            SourceLocation.Start,
            [
                new DeclarativeValidateSyntax(
                    [
                        new ValidationRuleSyntax(
                            ValidationRuleSyntax.ConceptValue,
                            ValidationRuleKind.Max,
                            new LiteralExpressionSyntax(value, SourceLocation.Start),
                            null,
                            SourceLocation.Start)
                    ],
                    SourceLocation.Start)
            ]);
}
