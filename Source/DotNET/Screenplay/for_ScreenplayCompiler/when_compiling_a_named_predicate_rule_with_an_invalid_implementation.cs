// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_named_predicate_rule_with_an_invalid_implementation : given.a_compiler
{
    const string Source =
        """
        module Customers
          feature Approval
            slice StateChange ApproveCustomer
              command ApproveCustomer
                orgNumber String

                validate
                  orgNumber rule BeAValidOrganizationNumber
                    not a file or a code block
        """;

    CompilationResult<ApplicationSyntax> _result;
    ValidationRuleSyntax _rule;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _rule = _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();
    }

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_one_diagnostic() => _result.Diagnostics.Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_still_keep_the_named_rule() => _rule.Rule.ShouldEqual(ValidationRuleKind.Rule);
    [Fact] void should_not_carry_a_file_reference() => _rule.File.ShouldBeNull();
    [Fact] void should_not_carry_inline_code() => _rule.Code.ShouldBeNull();
}
