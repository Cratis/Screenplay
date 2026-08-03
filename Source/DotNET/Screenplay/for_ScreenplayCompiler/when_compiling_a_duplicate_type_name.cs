// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_duplicate_type_name : given.a_compiler
{
    const string Source =
        """
        concept InvoiceLine : String

        type InvoiceLine
          quantity Int
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_duplicate() => _result.Diagnostics.Single().Message.ShouldEqual("Duplicate declaration of 'InvoiceLine' - concept and type names must be unique");
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
}
