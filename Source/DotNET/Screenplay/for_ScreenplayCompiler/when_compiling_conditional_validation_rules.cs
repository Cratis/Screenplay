// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_conditional_validation_rules : given.a_compiler
{
    const string Source =
        """
        module Engagements
          feature Extensions
            slice StateChange ExtendEngagement
              command ExtendEngagement
                startDate   Date
                endDate     Date
                isExtension Bool
                reason      String

                validate
                  endDate >= startDate
                  startDate < today
                  reason not empty when isExtension == true and startDate < today message "A reason is required"
                  reason matches "^when .+$"
        """;

    CompilationResult<ApplicationSyntax> _result;
    IEnumerable<ValidationRuleSyntax> _rules;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _rules = _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Rules;
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_resolve_a_path_operand_to_a_sibling_property() => ((PathExpressionSyntax)Rule(0).Value!).Path.ShouldEqual("startDate");
    [Fact] void should_resolve_today_to_the_keyword() => Rule(1).Value.ShouldBeOfExactType<TodayExpressionSyntax>();
    [Fact] void should_leave_an_unconditional_rule_unconditional() => Rule(0).When.ShouldBeNull();
    [Fact] void should_parse_the_rule_condition() => Rule(2).When.ShouldBeOfExactType<LogicalConditionSyntax>();
    [Fact] void should_keep_the_rule_itself() => Rule(2).Rule.ShouldEqual(ValidationRuleKind.NotEmpty);
    [Fact] void should_keep_the_message() => Rule(2).Message.ShouldEqual("A reason is required");
    [Fact] void should_not_read_when_inside_a_quoted_operand() => Rule(3).When.ShouldBeNull();
    [Fact] void should_keep_the_quoted_operand() => ((LiteralExpressionSyntax)Rule(3).Value!).Value.ShouldEqual("^when .+$");

    ValidationRuleSyntax Rule(int index) => _rules.ElementAt(index);
}
