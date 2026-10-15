// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_navigation_item_with_an_unknown_slot : given.a_navigation_document
{
    const string Shell =
        """
          navigation sidebar
            item a title "A" target Sales slot nowhere
        """;

    void Because() => CompileWith(Shell);

    [Fact] void should_report_1_navslotunknown() => Codes(DiagnosticCodes.NavSlotUnknown).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.NavSlotUnknown).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.NavSlotUnknown).First().Message.ShouldContain("nowhere");
}
