// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_duplicate_layouts : given.a_compiler
{
    const string Source =
        """
        layout AppShell
          content

        layout AppShell
          main
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_duplicate_layout() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateLayout);
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_only_keep_the_first_layout() => _result.Value!.Layouts!.Single().Slots.Select(slot => slot.Name).ShouldContainOnly("content");
}
