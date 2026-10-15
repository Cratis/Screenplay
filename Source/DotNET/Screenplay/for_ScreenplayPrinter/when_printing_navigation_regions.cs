// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_navigation_regions : given.a_printer
{
    const string Source =
        """
        layout AppShell
          category application
          topbar
          navigation contributes Navigation
          content
          side

          navigation topbar
            logo image "/acme.svg" title "Acme \"Co\""
            item sales title "Sales" icon receipt target Sales
            item support title $strings.nav.support target Support.Tickets slot side
          navigation sidebar
            item sales title "Sales" target Sales
              item orders title "Orders" target Sales.Orders
              item returns title "Returns" icon lucide-undo target Sales.Orders.Returns
            item "nav:help" title "Help" target Support

        module Sales
          navigation sidebar extends
            item customers title "Customers" target Sales.Customers
          navigation topbar overrides
            item orders title "Orders" target Sales.Orders

          feature Orders
            feature Returns
          feature Customers

        module Support
          navigation topbar inherits

          feature Tickets
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_keep_the_slots_before_the_regions() => _roundtrip.Printed.ShouldContain("  side\n  navigation topbar\n    logo image \"/acme.svg\" title \"Acme \\\"Co\\\"\"");
    [Fact] void should_print_an_item_with_every_part() => _roundtrip.Printed.ShouldContain("item support title $strings.nav.support target Support.Tickets slot side");
    [Fact] void should_print_sub_items_under_their_item() => _roundtrip.Printed.ShouldContain("    item sales title \"Sales\" target Sales\n      item orders title \"Orders\" target Sales.Orders\n      item returns title \"Returns\" icon lucide-undo target Sales.Orders.Returns");
    [Fact] void should_quote_an_id_that_is_not_an_identifier() => _roundtrip.Printed.ShouldContain("item \"nav:help\" title \"Help\" target Support");
    [Fact] void should_print_the_module_modes() => _roundtrip.Printed.ShouldContain("  navigation sidebar extends\n    item customers");
    [Fact] void should_print_an_override() => _roundtrip.Printed.ShouldContain("  navigation topbar overrides");
    [Fact] void should_print_an_explicit_inherit() => _roundtrip.Printed.ShouldContain("  navigation topbar inherits");
    [Fact] void should_preserve_the_layout_regions() => _roundtrip.Reparsed.Value!.Layouts!.Single().Navigation.Select(region => region.Region).ShouldContainOnly(NavigationRegionKind.Topbar, NavigationRegionKind.Sidebar);
    [Fact] void should_preserve_the_logo_text() => _roundtrip.Reparsed.Value!.Layouts!.Single().Navigation.First().Logo!.Title.ShouldEqual("Acme \"Co\"");
    [Fact] void should_preserve_the_module_modes() => _roundtrip.Reparsed.Value!.Modules.Single(module => module.Name == "Sales").Navigation.Select(region => region.Mode).ShouldContainOnly(NavigationRegionMode.Extend, NavigationRegionMode.Override);
    [Fact] void should_preserve_the_ids() => _roundtrip.Reparsed.Value!.Layouts!.Single().Navigation.Last().Items.Select(item => item.Id).ShouldContainOnly("sales", "nav:help");
}
