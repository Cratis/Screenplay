// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_toolbar_button_naming_an_ambiguous_slice : given.a_toolbar_document
{
    void Because() => CompileWith("button a title \"A\" slice PlaceOrder");

    [Fact] void should_report_toolbarbuttonsliceambiguous() => Codes(DiagnosticCodes.ToolbarButtonSliceAmbiguous).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.ToolbarButtonSliceAmbiguous).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.ToolbarButtonSliceAmbiguous).First().Message.ShouldContain("qualify");
}
