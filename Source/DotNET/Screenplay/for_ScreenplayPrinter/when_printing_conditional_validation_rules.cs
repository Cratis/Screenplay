// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_conditional_validation_rules : given.a_printer
{
    const string Source =
        """
        concept ExtensionReason : String
          validate
            not empty when isExtension == true message "A reason is required"

        module Engagements
          feature Extensions
            slice StateChange ExtendEngagement
              command ExtendEngagement
                startDate   Date
                endDate     Date
                @today      Date
                isExtension Bool

                validate
                  endDate >= startDate
                  startDate < today
                  startDate < @today
                  endDate > startDate when isExtension == true message "An extension has to move the end date out"
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_condition_before_the_message() => _roundtrip.Printed.ShouldContain("endDate > startDate when isExtension == true message \"An extension has to move the end date out\"");
    [Fact] void should_print_the_today_keyword() => _roundtrip.Printed.ShouldContain("startDate < today\n");
    [Fact] void should_escape_a_property_named_today() => _roundtrip.Printed.ShouldContain("startDate < @today");
    [Fact] void should_preserve_the_condition() => Rule(3).When.ShouldNotBeNull();
    [Fact] void should_preserve_the_today_keyword() => Rule(1).Value.ShouldBeOfExactType<TodayExpressionSyntax>();
    [Fact] void should_preserve_the_escaped_property() => ((PathExpressionSyntax)Rule(2).Value!).Path.ShouldEqual("today");
    [Fact] void should_preserve_the_concept_rule_condition() => ConceptRule().When.ShouldNotBeNull();

    ValidationRuleSyntax Rule(int index) =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.ElementAt(index);

    ValidationRuleSyntax ConceptRule() =>
        _roundtrip.Reparsed.Value!.Concepts.Single().Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();
}
