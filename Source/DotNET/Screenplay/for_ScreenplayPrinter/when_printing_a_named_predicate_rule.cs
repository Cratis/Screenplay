// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_named_predicate_rule : given.a_printer
{
    const string Source =
        """
        concept OrganizationNumber : String
          validate
            rule BeAValidOrganizationNumber message "Must be a valid organization number"

        module Customers
          feature Approval
            slice StateChange ApproveCustomer
              command ApproveCustomer
                orgNumber String

                validate
                  orgNumber not empty
                  orgNumber rule BeAValidOrganizationNumber message $strings.customers.orgNumberRequired
                  orgNumber rule BeUnique
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_named_rule() => _roundtrip.Printed.ShouldContain("orgNumber rule BeAValidOrganizationNumber message $strings.customers.orgNumberRequired");
    [Fact] void should_print_a_named_rule_without_a_message() => _roundtrip.Printed.ShouldContain("orgNumber rule BeUnique\n");
    [Fact] void should_print_the_implied_subject_form() => _roundtrip.Printed.ShouldContain("rule BeAValidOrganizationNumber message \"Must be a valid organization number\"");
    [Fact] void should_preserve_the_predicate_name() => ((PathExpressionSyntax)Rule(1).Value!).Path.ShouldEqual("BeAValidOrganizationNumber");
    [Fact] void should_preserve_the_concept_rule() => ConceptRule().Rule.ShouldEqual(ValidationRuleKind.Rule);

    ValidationRuleSyntax Rule(int index) =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.ElementAt(index);

    ValidationRuleSyntax ConceptRule() =>
        _roundtrip.Reparsed.Value!.Concepts.Single().Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();
}
