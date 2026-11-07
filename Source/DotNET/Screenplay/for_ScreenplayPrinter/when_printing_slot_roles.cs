// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_slot_roles : given.a_printer
{
    const string Source =
        """
        layout AppShell
          category Test
          topbar
          content page routed primary
          navigation panel
          outlet Navigation of navigation in navigation

        module Catalog
          screen template ListPage
            category Test
            fits slot content
            header
            main
            detail pane routed by sku optional

        ui profile Desktop
          target platform web
          root template AppShell
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_a_routed_primary_page() => _roundtrip.Printed.ShouldContain("  content page routed primary\n");
    [Fact] void should_print_a_docked_panel() => _roundtrip.Printed.ShouldContain("  navigation panel\n");
    [Fact] void should_print_an_optional_pane_routed_by_a_parameter() => _roundtrip.Printed.ShouldContain("    detail pane routed by sku optional\n");
    [Fact] void should_keep_a_slot_without_a_role_plain() => _roundtrip.Printed.ShouldContain("  topbar\n");
    [Fact] void should_read_the_page_role() => Slot("content").Role.ShouldEqual(SlotRole.Page);
    [Fact] void should_read_routed() => Slot("content").Routed.ShouldBeTrue();
    [Fact] void should_read_primary() => Slot("content").Primary.ShouldBeTrue();
    [Fact] void should_not_read_a_routing_parameter_that_is_not_there() => Slot("content").RoutedBy.ShouldBeNull();
    [Fact] void should_read_the_panel_role() => Slot("navigation").Role.ShouldEqual(SlotRole.Panel);
    [Fact] void should_read_the_pane_role() => Detail.Role.ShouldEqual(SlotRole.Pane);
    [Fact] void should_read_the_routing_parameter() => Detail.RoutedBy.ShouldEqual("sku");
    [Fact] void should_read_optional() => Detail.Optional.ShouldBeTrue();
    [Fact] void should_leave_a_plain_slot_without_a_role() => Slot("topbar").Role.ShouldBeNull();

    SlotSyntax Slot(string name) => _roundtrip.Reparsed.Value!.Layouts!.Single().Slots.Single(slot => slot.Name == name);

    SlotSyntax Detail => _roundtrip.Reparsed.Value!.Modules.Single().ScreenTemplates.Single().Slots.Single(slot => slot.Name == "detail");
}
