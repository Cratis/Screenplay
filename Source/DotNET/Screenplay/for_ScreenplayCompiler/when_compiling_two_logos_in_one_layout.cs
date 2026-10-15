// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_two_logos_in_one_layout : given.a_navigation_document
{
    const string Shell =
        """
          navigation topbar
            logo image "/a.svg" title "A"
          navigation sidebar
            logo image "/b.svg" title "B"
        """;

    void Because() => CompileWith(Shell);

    [Fact] void should_report_1_navigationlogonotallowed() => Codes(DiagnosticCodes.NavigationLogoNotAllowed).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.NavigationLogoNotAllowed).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.NavigationLogoNotAllowed).First().Message.ShouldContain("second logo");
}
