// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_navigation_region_with_an_invalid_header : given.a_navigation_document
{
    const string Shell =
        """
          navigation footer extends
            item a title "A" target Sales
        """;

    void Because() => CompileWith(Shell);

    [Fact] void should_report_1_invalidnavigationregiondeclaration() => Codes(DiagnosticCodes.InvalidNavigationRegionDeclaration).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.InvalidNavigationRegionDeclaration).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.InvalidNavigationRegionDeclaration).First().Message.ShouldContain("navigation footer");
}
