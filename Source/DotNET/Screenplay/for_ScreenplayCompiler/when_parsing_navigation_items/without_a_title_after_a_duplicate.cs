// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_navigation_items;

public class without_a_title_after_a_duplicate : Specification
{
    ParserContext _context;
    NavigationItemSyntax _item;

    void Establish() => _context = new(new(SourceLineSplitter.Split("item main icon home icon other")));
    void Because() => _item = NavigationParser.ParseItem(_context, _context.Reader.TakeSignificant())!;

    [Fact] void should_recover_with_an_empty_title() => _item.Title.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_first_icon() => _item.Icon.ShouldEqual("home");
    [Fact] void should_keep_both_diagnostic_codes() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([DiagnosticCodes.InvalidNavigationItem, DiagnosticCodes.InvalidNavigationItem]);
    [Fact] void should_report_the_missing_title_after_the_duplicate() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(["Navigation item 'main' states 'icon' more than once", "Navigation item 'main' has no title - expected 'item main title \"<text>\"'"]);
}
