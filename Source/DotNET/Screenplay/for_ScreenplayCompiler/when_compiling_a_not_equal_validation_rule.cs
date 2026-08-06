// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_not_equal_validation_rule : given.a_compiler
{
    const string Source =
        """
        concept InvoiceStatus : String
          validate
            != "unknown"  message "Status must be known"

        module Invoicing
          feature Invoices
            slice StateChange ChangeInvoiceStatus
              command ChangeInvoiceStatus
                status InvoiceStatus
                validate
                  status != "draft"     message "A draft cannot be published"
                  status == "sent"
                  lineCount != 0
        """;

    CompilationResult<ApplicationSyntax> _result;
    List<ValidationRuleSyntax> _commandRules;
    ValidationRuleSyntax _conceptRule;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _commandRules = [.. _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Rules];
        _conceptRule = _result.Value!.Concepts.Single().Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_not_equal_rule() => _commandRules[0].Rule.ShouldEqual(ValidationRuleKind.NotEqual);
    [Fact] void should_keep_the_property_the_rule_applies_to() => _commandRules[0].Property.ShouldEqual("status");
    [Fact] void should_keep_the_operand() => ((LiteralExpressionSyntax)_commandRules[0].Value!).Value.ShouldEqual("draft");
    [Fact] void should_keep_the_message() => _commandRules[0].Message.ShouldEqual("A draft cannot be published");
    [Fact] void should_still_parse_equal_as_equal() => _commandRules[1].Rule.ShouldEqual(ValidationRuleKind.Equal);
    [Fact] void should_parse_a_numeric_operand() => ((LiteralExpressionSyntax)_commandRules[2].Value!).Value.ShouldEqual(0d);
    [Fact] void should_parse_the_rule_on_a_concept() => _conceptRule.Rule.ShouldEqual(ValidationRuleKind.NotEqual);
    [Fact] void should_imply_the_concept_value_subject() => _conceptRule.Property.ShouldEqual(ValidationRuleSyntax.ConceptValue);
}
