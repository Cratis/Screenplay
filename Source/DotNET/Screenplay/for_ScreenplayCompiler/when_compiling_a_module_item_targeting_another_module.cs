// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_module_item_targeting_another_module : given.a_navigation_document
{
    const string SalesNavigation =
        """
          navigation sidebar extends
            item a title "A" target Support
        """;

    void Because() => CompileWith("", SalesNavigation);

    [Fact] void should_report_1_navtargetoutofscope() => Codes(DiagnosticCodes.NavTargetOutOfScope).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.NavTargetOutOfScope).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.NavTargetOutOfScope).First().Message.ShouldContain("only target that module");
}
