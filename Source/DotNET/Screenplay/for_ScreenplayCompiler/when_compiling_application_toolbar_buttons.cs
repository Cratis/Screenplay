// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

// Decision 0098 (#868): buttons belong to a layout's 'navigation topbar' region, once for the application, with the
// inherit, extend and override modes of the items.
public class when_compiling_application_toolbar_buttons : Specification
{
    const string Source =
        """
        policy IsBuyer
          require role "buyer"

        layout AppShell
          category application
          topbar
          navigation contributes Navigation
          content

          navigation topbar
            logo image "/acme.svg" title "Acme"
            item shop title "Shop" target Shop
            button newProduct title "New product" slice Shop.Orders.PlaceOrder emphasis primary icon plus
            button adjust title $strings.nav.adjust slice Orders.PlaceOrder
              authorize IsBuyer

        module Shop
          navigation topbar extends
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

    NavigationRegionSyntax Topbar => _result.Value!.Layouts!.Single().Navigation.Single();
    NavigationRegionSyntax ModuleTopbar => _result.Value!.Modules.Single().Navigation.Single();

    [Fact] void should_succeed() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(_ => _.Message)));
    [Fact] void should_report_nothing() => _result.Diagnostics.Where(_ => _.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
    [Fact] void should_read_the_application_buttons() => Topbar.Buttons.Select(button => button.Id).ShouldContainOnly("newProduct", "adjust");
    [Fact] void should_read_every_part_of_a_button() => (Topbar.Buttons.First().Title, Topbar.Buttons.First().Slice, Topbar.Buttons.First().Emphasis, Topbar.Buttons.First().Icon).ShouldEqual(("New product", "Shop.Orders.PlaceOrder", ToolbarEmphasis.Primary, "plus"));
    [Fact] void should_read_the_policy_that_gates_a_button() => Topbar.Buttons.Last().Authorize.ShouldNotBeNull();
    [Fact] void should_leave_a_button_without_authorize_open() => Topbar.Buttons.First().Authorize.ShouldBeNull();
    [Fact] void should_keep_the_items_beside_the_buttons() => Topbar.Items.Single().Id.ShouldEqual("shop");
    [Fact] void should_let_a_module_extend_the_buttons() => (ModuleTopbar.Mode, ModuleTopbar.Buttons.Single().Id).ShouldEqual((NavigationRegionMode.Extend, "more"));
}
