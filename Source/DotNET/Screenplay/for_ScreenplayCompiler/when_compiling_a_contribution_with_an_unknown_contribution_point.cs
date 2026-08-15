// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_contribution_with_an_unknown_contribution_point : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          contribute to Ghost
            label "Nothing"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_a_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_unknown_contribution_point() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownContributionPoint);
    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_the_contribution_point() => _result.Diagnostics.Single().Message.ShouldContain("Ghost");
}
