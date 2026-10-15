// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_overriding_region_without_items : given.a_navigation_document
{
    const string SalesNavigation =
        """
          navigation sidebar overrides
        """;

    void Because() => CompileWith("", SalesNavigation);

    [Fact] void should_report_1_navoverridewithoutitems() => Codes(DiagnosticCodes.NavOverrideWithoutItems).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_a_warning() => Codes(DiagnosticCodes.NavOverrideWithoutItems).First().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.NavOverrideWithoutItems).First().Message.ShouldContain("without items");
}
