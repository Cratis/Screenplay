// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_application_region_with_a_mode : given.a_navigation_document
{
    const string Shell =
        """
          navigation sidebar extends
            item a title "A" target Sales
        """;

    void Because() => CompileWith(Shell);

    [Fact] void should_report_1_navigationregionmodenotallowed() => Codes(DiagnosticCodes.NavigationRegionModeNotAllowed).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.NavigationRegionModeNotAllowed).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.NavigationRegionModeNotAllowed).First().Message.ShouldContain("base");
}
