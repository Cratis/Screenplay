// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_persona_with_an_unknown_policy : given.a_compiler
{
    const string Source =
        """
        persona Accountant
          policy IsAccountant

        module Invoicing
          feature Invoices
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_reject_the_unknown_policy() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_unknown_policy() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown policy 'IsAccountant' - declare it with 'policy IsAccountant'");
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
}
