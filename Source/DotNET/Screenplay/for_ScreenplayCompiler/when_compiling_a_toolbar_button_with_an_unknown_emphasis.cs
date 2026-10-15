// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_toolbar_button_with_an_unknown_emphasis : given.a_toolbar_document
{
    void Because() => CompileWith("button a title \"A\" slice Shop.Orders.PlaceOrder emphasis loud");

    [Fact] void should_report_invalidtoolbarbutton() => Codes(DiagnosticCodes.InvalidToolbarButton).Count().ShouldEqual(1);
    [Fact] void should_report_it_as_an_error() => Codes(DiagnosticCodes.InvalidToolbarButton).First().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_what_is_wrong() => Codes(DiagnosticCodes.InvalidToolbarButton).First().Message.ShouldContain("loud");
}
