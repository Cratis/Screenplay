// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_specification_with_an_invalid_readmodel : given.a_compiler
{
    const string Source =
        """
        specification ShowingAnExistingInvoice
          given readmodel
            status = "draft"
          then readmodel lowercase
            status = "sent"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_invalid_given() => _result.Diagnostics.First().Message.ShouldEqual("Invalid 'given readmodel' declaration 'given readmodel' - expected 'given readmodel <ReadModelType>'");
    [Fact] void should_report_the_invalid_then() => _result.Diagnostics.Last().Message.ShouldEqual("Invalid 'then readmodel' declaration 'then readmodel lowercase' - expected 'then readmodel <ReadModelType>'");
    [Fact] void should_report_errors_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
}
