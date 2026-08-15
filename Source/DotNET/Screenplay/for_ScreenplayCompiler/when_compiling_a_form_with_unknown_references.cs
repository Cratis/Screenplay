// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_form_with_unknown_references : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          form OrphanForm for GhostCommand
            populate via query GhostQuery
            field id

            on submit navigate to GhostScreen
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_three_diagnostics() => _result.Diagnostics.Count().ShouldEqual(3);
    [Fact] void should_report_the_unknown_command() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownCommand).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_report_the_unknown_query() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownQuery).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_report_the_unknown_screen() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownScreen).Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
