// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_invalid_then_error : given.a_compiler
{
    const string Source =
        """
        specification WhenExchangingAndMagicLinkIsNotActive
          when ExchangeToken
          then error not-a-string
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_invalid_form() => _result.Diagnostics.Single().Message.ShouldEqual("Invalid 'then error' declaration 'then error not-a-string' - expected 'then error' or 'then error \"<reason>\"'");
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
}
