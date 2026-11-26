// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_navigation_items;

public class with_invalid_parts_followed_by_valid_values : Specification
{
    const string Source = """
        item main title bare title $strings.nav.main screen "Page" screen Page slot "body" slot body target "Shop.Orders" target Shop.Orders
        """;

    ParserContext _context;
    NavigationItemSyntax _item;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source, path: "navigation.play")));
    void Because() => _item = NavigationParser.ParseItem(_context, _context.Reader.TakeSignificant())!;

    [Fact] void should_read_the_later_localized_title() => _item.Title.ShouldEqual("$strings.nav.main");
    [Fact] void should_read_the_later_slot() => _item.Slot.ShouldEqual("body");
    [Fact] void should_apply_the_screen_to_the_later_target() => (_item.Target!.Module, _item.Target.Feature, _item.Target.Screen).ShouldEqual(("Shop", "Orders", "Page"));
    [Fact] void should_report_only_the_invalid_values_not_duplicates() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(Enumerable.Repeat(DiagnosticCodes.InvalidNavigationItem, 4));
    [Fact] void should_keep_the_validation_messages_in_order() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(["Invalid title 'bare' on navigation item 'main' - expected a quoted string or a $strings.<key> token", "Invalid screen 'Page' on navigation item 'main' - expected a screen name", "Invalid slot 'body' on navigation item 'main' - expected a slot name", "Invalid target 'Shop.Orders' on navigation item 'main' - expected <Module> or <Module>.<Feature>"]);
}
