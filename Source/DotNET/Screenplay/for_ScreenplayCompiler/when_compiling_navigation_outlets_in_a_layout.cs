// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_navigation_outlets_in_a_layout : given.a_navigation_document
{
    const string Shell =
        """
          outlet topbar of navigation
            logo image "/acme.svg" title "Acme"
            default sales title "Sales" icon receipt target Sales
            default support title "Support" target Support.Tickets slot content
          outlet sidebar of navigation
            default sales title "Sales" target Sales
              item orders title "Orders" target Sales.Orders
              item returns title "Returns" target Sales.Orders.Returns slot side
            default help title $strings.nav.help target Support
        """;

    void Because() => CompileWith(Shell);

    LayoutSyntax Layout => _result.Value!.Layouts!.Single();
    OutletSyntax Topbar => Layout.Outlets.Single(outlet => outlet.Name == "topbar");
    OutletSyntax Sidebar => Layout.Outlets.Single(outlet => outlet.Name == "sidebar");
    NavigationItemSyntax Item(OutletSyntax outlet, int index) => (NavigationItemSyntax)outlet.Items.ElementAt(index);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_slots_the_layout_declares() => Layout.Slots.Select(slot => slot.Name).ShouldContainOnly("topbar", "navigation", "content", "side");
    [Fact] void should_read_every_outlet() => Layout.Outlets.Select(outlet => outlet.Name).ShouldContainOnly("Navigation", "topbar", "sidebar");
    [Fact] void should_read_the_navigation_kind() => Layout.Outlets.All(outlet => outlet.Kind == OutletKind.Navigation).ShouldBeTrue();
    [Fact] void should_leave_the_declarations_without_a_mode() => Layout.Outlets.All(outlet => outlet.Mode == OutletMode.Unspecified).ShouldBeTrue();
    [Fact] void should_read_the_items_as_defaults() => Topbar.Items.All(item => item.IsDefault).ShouldBeTrue();
    [Fact] void should_read_the_logo() => (Topbar.Logo!.Image, Topbar.Logo.Title).ShouldEqual(("/acme.svg", "Acme"));
    [Fact] void should_read_the_item_title_icon_and_module_target() => (Item(Topbar, 0).Title, Item(Topbar, 0).Icon, Item(Topbar, 0).Target!.Module, Item(Topbar, 0).Target!.Feature).ShouldEqual(("Sales", "receipt", "Sales", null));
    [Fact] void should_read_the_feature_target_and_slot() => (Item(Topbar, 1).Target!.Feature, Item(Topbar, 1).Slot).ShouldEqual(("Tickets", "content"));
    [Fact] void should_read_sub_items_under_an_item() => Item(Sidebar, 0).Children.Select(child => child.Id).ShouldContainOnly("orders", "returns");
    [Fact] void should_read_a_nested_feature_path() => Item(Sidebar, 0).Children.Last().Target!.Feature.ShouldEqual("Orders.Returns");
    [Fact] void should_read_a_localized_title_as_written() => Item(Sidebar, 1).Title.ShouldEqual("$strings.nav.help");
}
