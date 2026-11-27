// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_navigation_items;

public class with_an_unexpected_tail : Specification
{
    const string Source = "item main icon home colour red title \"Ignored\"";

    ParserContext _context;
    NavigationItemSyntax _item;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));
    void Because() => _item = NavigationParser.ParseItem(_context, _context.Reader.TakeSignificant())!;

    [Fact] void should_keep_the_icon_read_before_the_tail() => _item.Icon.ShouldEqual("home");
    [Fact] void should_not_read_the_title_past_the_unexpected_tail() => _item.Title.ShouldEqual(string.Empty);
    [Fact] void should_not_add_a_missing_title_diagnostic() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidNavigationItem);
    [Fact] void should_keep_the_unexpected_tail_message() => _context.Diagnostics.Single().Message.ShouldEqual($"Invalid navigation item '{Source}' - unexpected 'colour red title \"Ignored\"'; expected {NavigationParser.Expected}");
}
