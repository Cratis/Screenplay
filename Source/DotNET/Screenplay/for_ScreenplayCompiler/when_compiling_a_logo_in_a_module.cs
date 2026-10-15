// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_logo_in_a_module : given.a_navigation_document
{
    const string SalesNavigation =
        """
          navigation topbar extends
            logo image "/x.svg" title "X"
            item a title "A" target Sales
        """;

    void Because() => CompileWith("", SalesNavigation);

    [Fact] void should_report_1_navigationlogonotallowed() => Codes(DiagnosticCodes.NavigationLogoNotAllowed).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.NavigationLogoNotAllowed).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.NavigationLogoNotAllowed).First().Message.ShouldContain("application");
}
