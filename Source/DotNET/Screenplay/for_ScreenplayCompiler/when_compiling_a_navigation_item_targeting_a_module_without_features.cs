// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_navigation_item_targeting_a_module_without_features : given.a_navigation_document
{
    const string Shell =
        """
          navigation sidebar
            item a title "A" target Empty
        """;

    void Because() => CompileWith(Shell);

    [Fact] void should_report_1_navmodulewithoutfeatures() => Codes(DiagnosticCodes.NavModuleWithoutFeatures).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_a_warning() => Codes(DiagnosticCodes.NavModuleWithoutFeatures).First().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.NavModuleWithoutFeatures).First().Message.ShouldContain("no feature");
}
