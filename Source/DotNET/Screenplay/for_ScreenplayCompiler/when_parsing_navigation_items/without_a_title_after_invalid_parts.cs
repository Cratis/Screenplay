// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_navigation_items;

public class without_a_title_after_invalid_parts : Specification
{
    const string Source = "item main screen $strings.nav.page slot body-name target Shop..Orders";

    ParserContext _context;
    NavigationItemSyntax _item;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));
    void Because() => _item = NavigationParser.ParseItem(_context, _context.Reader.TakeSignificant())!;

    [Fact] void should_recover_with_an_empty_title() => _item.Title.ShouldEqual(string.Empty);
    [Fact] void should_leave_the_slot_unset() => _item.Slot.ShouldBeNull();
    [Fact] void should_leave_the_target_unset() => _item.Target.ShouldBeNull();
    [Fact] void should_not_add_a_missing_title_or_screen_target_diagnostic() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(Enumerable.Repeat(DiagnosticCodes.InvalidNavigationItem, 3));
    [Fact] void should_report_each_invalid_value_in_order() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(["Invalid screen '$strings.nav.page' on navigation item 'main' - expected a screen name", "Invalid slot 'body-name' on navigation item 'main' - expected a slot name", "Invalid target 'Shop..Orders' on navigation item 'main' - expected <Module> or <Module>.<Feature>"]);
}
