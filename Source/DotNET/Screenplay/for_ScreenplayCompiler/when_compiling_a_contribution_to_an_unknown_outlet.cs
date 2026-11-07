// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_contribution_to_an_unknown_outlet : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          contribute to Ghost
            label "Nothing"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_unknown_outlet() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownOutlet);
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_the_outlet() => _result.Diagnostics.Single().Message.ShouldContain("Ghost");
}
