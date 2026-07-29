// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_named_predicate_rule : given.a_compiler
{
    const string Source =
        """
        module Customers
          feature Approval
            slice StateChange ApproveCustomer
              command ApproveCustomer
                orgNumber String

                validate
                  orgNumber not empty                       message "Organization number is required"
                  orgNumber rule BeAValidOrganizationNumber message $strings.customers.orgNumberRequired
                  orgNumber rule BeUnique
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
    [Fact] void should_keep_every_rule() => _rules.Count().ShouldEqual(3);
    [Fact] void should_recognize_the_named_rule() => Rule(1).Rule.ShouldEqual(ValidationRuleKind.Rule);
    [Fact] void should_carry_the_predicate_name() => ((PathExpressionSyntax)Rule(1).Value!).Path.ShouldEqual("BeAValidOrganizationNumber");
    [Fact] void should_keep_the_localized_message() => Rule(1).Message.ShouldEqual("$strings.customers.orgNumberRequired");
    [Fact] void should_allow_a_named_rule_without_a_message() => Rule(2).Message.ShouldBeNull();
    [Fact] void should_keep_the_subject_of_the_named_rule() => Rule(2).Property.ShouldEqual("orgNumber");

    ValidationRuleSyntax Rule(int index) => _rules.ElementAt(index);
}
