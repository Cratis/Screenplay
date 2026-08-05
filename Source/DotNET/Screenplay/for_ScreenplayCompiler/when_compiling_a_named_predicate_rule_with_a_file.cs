// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_named_predicate_rule_with_a_file : given.a_compiler
{
    const string Source =
        """
        module Customers
          feature Approval
            slice StateChange ApproveCustomer
              command ApproveCustomer
                orgNumber String

                validate
                  orgNumber rule BeAValidOrganizationNumber message "Must be a valid organization number"
                    file Validations/BeAValidOrganizationNumber.cs
        """;

    CompilationResult<ApplicationSyntax> _result;
    ValidationRuleSyntax _rule;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _rule = _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_file_reference() => _rule.File!.Path.ShouldEqual("Validations/BeAValidOrganizationNumber.cs");
    [Fact] void should_not_carry_inline_code() => _rule.Code.ShouldBeNull();
}
