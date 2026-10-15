// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_navigation_item_without_a_target : given.a_navigation_document
{
    const string Shell =
        """
          navigation sidebar
            item a title "A"
        """;

    void Because() => CompileWith(Shell);

    [Fact] void should_report_1_navtargetmissing() => Codes(DiagnosticCodes.NavTargetMissing).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.NavTargetMissing).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.NavTargetMissing).First().Message.ShouldContain("no target");
}
