// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_toolbar_button_reusing_an_item_name : given.a_toolbar_document
{
    void Because() => CompileWith("item a action PlaceOrder", "button a title \"B\" slice Billing.PlaceOrder");

    [Fact] void should_report_duplicatetoolbarbutton() => Codes(DiagnosticCodes.DuplicateToolbarButton).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.DuplicateToolbarButton).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.DuplicateToolbarButton).First().Message.ShouldContain("already has");
}
