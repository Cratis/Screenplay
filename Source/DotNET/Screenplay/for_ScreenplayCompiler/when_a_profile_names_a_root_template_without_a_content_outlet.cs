// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_profile_names_a_root_template_without_a_content_outlet : given.a_compiler
{
    const string Source =
        """
        layout Hollow
          category Shell
          topbar
          footer

        ui profile Desktop
          root template Hollow
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_missing_outlet() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateWithoutContentOutlet).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(7);
    [Fact] void should_name_the_outlet() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateWithoutContentOutlet).Single().Message.ShouldContain("no 'content' outlet");
    [Fact] void should_report_an_error() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RootTemplateWithoutContentOutlet).Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
}
