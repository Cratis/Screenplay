// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_not_equal_validation_rule : given.a_printer
{
    const string Source =
        """
        concept InvoiceStatus : String
          validate
            != "unknown" message "Status must be known"

        module Invoicing
          feature Invoices
            slice StateChange ChangeInvoiceStatus
              command ChangeInvoiceStatus
                status InvoiceStatus
                validate
                  status != "draft" message "A draft cannot be published"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_rule() => _roundtrip.Printed.ShouldContain(@"status != ""draft"" message ""A draft cannot be published""");
    [Fact] void should_print_the_concept_rule_without_a_subject() => _roundtrip.Printed.ShouldContain(@"!= ""unknown"" message ""Status must be known""");
    [Fact] void should_preserve_the_rule_kind() => CommandRule.Rule.ShouldEqual(ValidationRuleKind.NotEqual);
    [Fact] void should_preserve_the_operand() => ((LiteralExpressionSyntax)CommandRule.Value!).Value.ShouldEqual("draft");
    [Fact] void should_preserve_the_concept_rule_kind() => ConceptRule.Rule.ShouldEqual(ValidationRuleKind.NotEqual);

    ValidationRuleSyntax CommandRule =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();

    ValidationRuleSyntax ConceptRule =>
        _roundtrip.Reparsed.Value!.Concepts.Single().Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();
}
