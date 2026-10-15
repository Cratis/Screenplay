// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_nested_items_in_a_topbar : given.a_navigation_document
{
    const string Shell =
        """
          navigation topbar
            item a title "A" target Sales
              item b title "B" target Sales.Orders
        """;

    void Because() => CompileWith(Shell);

    [Fact] void should_report_1_navigationchildrennotallowed() => Codes(DiagnosticCodes.NavigationChildrenNotAllowed).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.NavigationChildrenNotAllowed).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.NavigationChildrenNotAllowed).First().Message.ShouldContain("only the sidebar");
}
