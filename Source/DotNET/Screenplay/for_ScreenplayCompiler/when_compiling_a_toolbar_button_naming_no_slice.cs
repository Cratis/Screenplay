// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_toolbar_button_naming_no_slice : given.a_toolbar_document
{
    void Because() => CompileWith("button a title \"A\"");

    [Fact] void should_report_toolbarbuttonwithoutslice() => Codes(DiagnosticCodes.ToolbarButtonWithoutSlice).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_a_warning() => Codes(DiagnosticCodes.ToolbarButtonWithoutSlice).First().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.ToolbarButtonWithoutSlice).First().Message.ShouldContain("names no slice");
}
