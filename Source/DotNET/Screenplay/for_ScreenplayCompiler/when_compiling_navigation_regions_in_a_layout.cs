// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_navigation_regions_in_a_layout : given.a_navigation_document
{
    const string Shell =
        """
          navigation topbar
            logo image "/acme.svg" title "Acme"
            item sales title "Sales" icon receipt target Sales
            item support title "Support" target Support.Tickets slot content
          navigation sidebar
            item sales title "Sales" target Sales
              item orders title "Orders" target Sales.Orders
              item returns title "Returns" target Sales.Orders.Returns slot side
            item help title $strings.nav.help target Support
        """;

    void Because() => CompileWith(Shell);

    LayoutSyntax Layout => _result.Value!.Layouts!.Single();
    NavigationRegionSyntax Topbar => Layout.Navigation.Single(region => region.Region == NavigationRegionKind.Topbar);
    NavigationRegionSyntax Sidebar => Layout.Navigation.Single(region => region.Region == NavigationRegionKind.Sidebar);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_slots_the_layout_declares() => Layout.Slots.Select(slot => slot.Name).ShouldContainOnly("topbar", "navigation", "content", "side");
    [Fact] void should_read_both_regions() => Layout.Navigation.Count().ShouldEqual(2);
    [Fact] void should_leave_the_application_regions_without_a_mode() => Layout.Navigation.All(region => region.Mode == NavigationRegionMode.Unspecified).ShouldBeTrue();
    [Fact] void should_read_the_logo() => (Topbar.Logo!.Image, Topbar.Logo.Title).ShouldEqual(("/acme.svg", "Acme"));
    [Fact] void should_read_the_item_title_icon_and_module_target() => (Topbar.Items.First().Title, Topbar.Items.First().Icon, Topbar.Items.First().Target!.Module, Topbar.Items.First().Target!.Feature).ShouldEqual(("Sales", "receipt", "Sales", null));
    [Fact] void should_read_the_feature_target_and_slot() => (Topbar.Items.Last().Target!.Feature, Topbar.Items.Last().Slot).ShouldEqual(("Tickets", "content"));
    [Fact] void should_read_sub_items_under_a_sidebar_item() => Sidebar.Items.First().Children.Select(child => child.Id).ShouldContainOnly("orders", "returns");
    [Fact] void should_read_a_nested_feature_path() => Sidebar.Items.First().Children.Last().Target!.Feature.ShouldEqual("Orders.Returns");
    [Fact] void should_read_a_localized_title_as_written() => Sidebar.Items.Last().Title.ShouldEqual("$strings.nav.help");
}
