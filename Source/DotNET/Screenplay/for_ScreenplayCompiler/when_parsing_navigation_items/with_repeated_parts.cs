// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_navigation_items;

public class with_repeated_parts : Specification
{
    const string Source = """
        item main title "First" title "Second" icon "" icon home target Shop.Orders target Other.Feature screen Page screen Other slot body slot side
        """;

    ParserContext _context;
    NavigationItemSyntax _item;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source, path: "navigation.play")));
    void Because() => _item = NavigationParser.ParseItem(_context, _context.Reader.TakeSignificant())!;

    [Fact] void should_keep_the_first_title() => _item.Title.ShouldEqual("First");
    [Fact] void should_treat_an_empty_icon_as_already_present() => _item.Icon.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_first_target_and_screen() => (_item.Target!.Module, _item.Target.Feature, _item.Target.Screen).ShouldEqual(("Shop", "Orders", "Page"));
    [Fact] void should_keep_the_first_slot() => _item.Slot.ShouldEqual("body");
    [Fact] void should_keep_the_diagnostic_codes() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(Enumerable.Repeat(DiagnosticCodes.InvalidNavigationItem, 5));
    [Fact] void should_keep_the_diagnostic_severities() => _context.Diagnostics.Select(diagnostic => diagnostic.Severity).ShouldEqual(Enumerable.Repeat(DiagnosticSeverity.Error, 5));
    [Fact] void should_locate_every_duplicate_on_the_item() => _context.Diagnostics.Select(diagnostic => diagnostic.Location).ShouldEqual(Enumerable.Repeat(new SourceLocation(1, 1, "navigation.play"), 5));
    [Fact] void should_report_duplicates_in_authored_order() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(["Navigation item 'main' states 'title' more than once", "Navigation item 'main' states 'icon' more than once", "Navigation item 'main' states 'target' more than once", "Navigation item 'main' states 'screen' more than once", "Navigation item 'main' states 'slot' more than once"]);
}
