// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_empty_type : given.a_compiler
{
    const string Source =
        """
        type InvoiceLine
          description "Nothing but a description"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_missing_properties() => _result.Diagnostics.Single().Message.ShouldEqual("Type 'InvoiceLine' must declare at least one property");
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
}
