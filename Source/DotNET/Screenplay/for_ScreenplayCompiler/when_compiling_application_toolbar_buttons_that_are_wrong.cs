// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_application_toolbar_buttons_that_are_wrong : Specification
{
    static string Model(string outlet, string button) => $$"""
        layout AppShell
          category application
          topbar
          navigation
          content

          outlet appActions of actions in topbar
          outlet side of navigation in navigation

        module Shop
          outlet {{outlet}} extends
            {{button}}
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
              screen PlaceOrder
                action PlaceOrder
        """;

    static IEnumerable<string> Codes(string source) => new ScreenplayCompiler().Compile(source).Diagnostics.Where(_ => _.Severity != DiagnosticSeverity.Information).Select(_ => _.Code);

    [Fact] void should_refuse_a_button_in_a_navigation_outlet() => Codes(Model("side", "button b title \"B\" slice PlaceOrder")).ShouldContain(DiagnosticCodes.OutletItemWrongKind);
    [Fact] void should_refuse_an_unknown_slice() => Codes(Model("appActions", "button b title \"B\" slice Nope")).ShouldContain(DiagnosticCodes.ToolbarButtonSliceNotFound);
    [Fact] void should_warn_about_a_button_without_a_slice() => Codes(Model("appActions", "button b title \"B\"")).ShouldContain(DiagnosticCodes.ToolbarButtonWithoutSlice);
    [Fact] void should_refuse_a_button_without_a_title() => Codes(Model("appActions", "button b slice PlaceOrder")).ShouldContain(DiagnosticCodes.InvalidToolbarButton);
    [Fact] void should_refuse_a_duplicate_id() => new ScreenplayCompiler().Compile(Model("appActions", "button b title \"B\" slice PlaceOrder\n    button b title \"C\" slice PlaceOrder")).Diagnostics.Select(_ => _.Code).ShouldContain(DiagnosticCodes.DuplicateOutletItem);
    [Fact] void should_refuse_anything_but_authorize_under_a_button() => Codes(Model("appActions", "button b title \"B\" slice PlaceOrder\n      item x title \"X\"")).ShouldContain(DiagnosticCodes.InvalidToolbarButton);
}
