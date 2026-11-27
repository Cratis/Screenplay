// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets.when_reading_retired_navigation_regions;

public class with_malformed_and_repeated_children : Specification
{
    const string Source = """
        navigation sidebar overrides
          logo image "first.svg" title "First"
          logo title "Broken"
            item nested title "Nested"
          logo image "later.svg" title "Later"
          item 123
            item hidden title "Hidden"
          button 123
            button hidden title "Hidden"
          divider
            item hidden title "Hidden"
          item orders title bare target Shop.Orders
          button help title "Help" runs close
        navigation topbar
        """;

    ParserContext _context;
    RetiredNavigationRegion _region;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source, path: "navigation.play")));

    void Because() => _region = NavigationParser.ReportRegion(_context, _context.Reader.TakeSignificant(), "module");

    [Fact] void should_keep_the_region_and_mode() => (_region.Region, _region.Mode).ShouldEqual(("sidebar", (OutletMode?)OutletMode.Override));
    [Fact] void should_keep_the_first_valid_logo() => (_region.Logo!.Image, _region.Logo.Title).ShouldEqual(("first.svg", "First"));
    [Fact] void should_keep_the_logo_location() => _region.Logo!.Location.ShouldEqual(new SourceLocation(2, 3, "navigation.play"));
    [Fact] void should_keep_items_and_buttons_in_authored_order() => _region.Items.Select(item => item.Id).ShouldEqual(["orders", "help"]);
    [Fact] void should_keep_the_item_type() => _region.Items.First().ShouldBeOfExactType<NavigationItemSyntax>();
    [Fact] void should_keep_an_item_with_invalid_parts_for_migration() => _region.Items.First().Title.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_button_type() => _region.Items.Last().ShouldBeOfExactType<ToolbarButtonSyntax>();
    [Fact] void should_keep_unrecognized_lines_in_order() => _region.Unrecognized.ShouldEqual(new SourceLocation(3, 3, "navigation.play"), new SourceLocation(5, 3, "navigation.play"), new SourceLocation(6, 3, "navigation.play"), new SourceLocation(8, 3, "navigation.play"), new SourceLocation(10, 3, "navigation.play"));
    [Fact] void should_not_leak_quiet_child_diagnostics() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.RemovedNavigationRegion);
    [Fact] void should_keep_the_retirement_message() => _context.Diagnostics.Single().Message.ShouldEqual("'navigation sidebar overrides' in a module is written 'outlet sidebar overrides' since Screenplay 6 - an item list a template offers is an outlet; run 'screenplay migrate --from 5 --to 6' (the slots-and-outlets step) to rewrite the model");
    [Fact] void should_locate_the_retirement_on_the_header() => _context.Diagnostics.Single().Location.ShouldEqual(new SourceLocation(1, 1, "navigation.play"));
    [Fact] void should_report_retirement_as_an_error() => _context.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_leave_the_next_region_unconsumed() => _context.Reader.PeekSignificant()!.Content.ShouldEqual("navigation topbar");
}
