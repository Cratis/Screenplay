// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

// Decision 0098 (#868) put the application's buttons in the layout once; Screenplay 6 declares them as an actions outlet,
// which modules inherit, extend or override like any outlet.
public class when_compiling_application_toolbar_buttons : Specification
{
    const string Source =
        """
        policy IsBuyer
          require role "buyer"

        layout AppShell
          category application
          topbar
          navigation
          content

          outlet topbar of navigation in topbar
            logo image "/acme.svg" title "Acme"
            default shop title "Shop" target Shop
          outlet appActions of actions in topbar
            default newProduct title "New product" slice Shop.Orders.PlaceOrder emphasis primary icon plus
            default adjust title $strings.nav.adjust slice Orders.PlaceOrder
              authorize IsBuyer

        module Shop
          outlet appActions extends
            button more title "More" slice PlaceOrder emphasis quiet
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    OutletSyntax Topbar => _result.Value!.Layouts!.Single().Outlets.Single(outlet => outlet.Name == "topbar");
    IEnumerable<ToolbarButtonSyntax> Buttons => _result.Value!.Layouts!.Single().Outlets.Single(outlet => outlet.Name == "appActions").Items.Cast<ToolbarButtonSyntax>();
    OutletSyntax ModuleActions => _result.Value!.Modules.Single().Outlets.Single();

    [Fact] void should_succeed() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(_ => _.Message)));
    [Fact] void should_report_nothing() => _result.Diagnostics.Where(_ => _.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_read_the_application_buttons() => Buttons.Select(button => button.Id).ShouldContainOnly("newProduct", "adjust");
    [Fact] void should_read_every_part_of_a_button() => (Buttons.First().Title, Buttons.First().Slice, Buttons.First().Emphasis, Buttons.First().Icon).ShouldEqual(("New product", "Shop.Orders.PlaceOrder", ToolbarEmphasis.Primary, "plus"));
    [Fact] void should_read_the_policy_that_gates_a_button() => Buttons.Last().Authorize.ShouldNotBeNull();
    [Fact] void should_leave_a_button_without_authorize_open() => Buttons.First().Authorize.ShouldBeNull();
    [Fact] void should_keep_the_navigation_beside_the_buttons() => Topbar.Items.Single().Id.ShouldEqual("shop");
    [Fact] void should_let_a_module_extend_the_buttons() => (ModuleActions.Mode, ModuleActions.Items.Single().Id).ShouldEqual((OutletMode.Extend, "more"));
}
