// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_duplicate_ui_profiles : given.a_compiler
{
    const string Source =
        """
        ui profile Desktop
          target platform web

        ui profile Desktop
          target platform web
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_duplicate_profile() => _result.Diagnostics.Single().Message.ShouldEqual("A ui profile named 'Desktop' is already declared - profile names must be unique");
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_keep_the_first_profile() => _result.Value!.UiProfiles!.Single().Platforms.ShouldContainOnly("web");
}
