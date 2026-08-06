// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_named_predicate_rule_with_inline_code : given.a_compiler
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
                    csharp
                      ```
                      string orgNumber = context.Value;
                      return orgNumber.Length == 9 && orgNumber.All(char.IsDigit);
                      ```
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
    [Fact] void should_carry_the_code_language() => _rule.Code!.Language.ShouldEqual("csharp");
    [Fact] void should_carry_the_inline_code() => _rule.Code!.Code.ShouldContain("orgNumber.Length == 9");
    [Fact] void should_not_carry_a_file_reference() => _rule.File.ShouldBeNull();
}
