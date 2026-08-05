// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_named_predicate_rule_without_a_name : given.a_compiler
{
    const string Source =
        """
        module Customers
          feature Approval
            slice StateChange ApproveCustomer
              command ApproveCustomer
                orgNumber String

                validate
                  orgNumber rule
                  orgNumber rule "not an identifier"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_both_rules() => _result.Diagnostics.Count().ShouldEqual(2);
    [Fact] void should_report_them_as_errors() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_not_keep_any_rule() => _result.Value!.Modules.Single().Features.Single().Slices.Single()
        .Commands.Single().Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.ShouldBeEmpty();
}
