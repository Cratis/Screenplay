// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_navigation_items;

public class with_nested_items_and_repeated_positions : Specification
{
    const string Source = """
        item main title "Main"
          item 123
            item hidden title "Hidden"
          item first title "First"
            before other
          divider
            item hidden title "Hidden"
          before other
          after later
          item last title "Last"
        item sibling title "Sibling"
        """;

    ParserContext _context;
    NavigationItemSyntax _item;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source, path: "navigation.play")));
    void Because() => _item = NavigationParser.ParseItem(_context, _context.Reader.TakeSignificant(), allowPosition: true)!;

    [Fact] void should_keep_only_the_valid_children_in_order() => _item.Children.Select(child => child.Id).ShouldEqual(["first", "last"]);
    [Fact] void should_not_apply_the_parents_position_permission_to_a_child() => _item.Children.First().Position.ShouldBeNull();
    [Fact] void should_keep_the_first_position() => (_item.Position!.Placement, _item.Position.Id).ShouldEqual((OutletPlacement.Before, "other"));
    [Fact] void should_keep_the_position_location() => _item.Position!.Location.ShouldEqual(new SourceLocation(8, 3, "navigation.play"));
    [Fact] void should_leave_the_next_sibling_unconsumed() => _context.Reader.PeekSignificant()!.Content.ShouldEqual("item sibling title \"Sibling\"");
    [Fact] void should_keep_the_diagnostic_order() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([DiagnosticCodes.InvalidNavigationItem, DiagnosticCodes.InvalidNavigationItem, DiagnosticCodes.InvalidNavigationItem, DiagnosticCodes.InvalidOutletItem]);
    [Fact] void should_keep_the_diagnostic_locations() => _context.Diagnostics.Select(diagnostic => diagnostic.Location).ShouldEqual([new SourceLocation(2, 3, "navigation.play"), new SourceLocation(5, 5, "navigation.play"), new SourceLocation(6, 3, "navigation.play"), new SourceLocation(9, 3, "navigation.play")]);
    [Fact] void should_keep_the_recovery_messages() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual([$"Invalid navigation item 'item 123' - expected {NavigationParser.Expected}", "Unexpected 'before other' under navigation item 'first' - only sub-items are allowed: item <id> title \"<text>\" ...", "Unexpected 'divider' under navigation item 'main' - only sub-items are allowed: item <id> title \"<text>\" ...", "Item 'main' already says where it stands - write one 'before <id>' or 'after <id>'"]);
}
