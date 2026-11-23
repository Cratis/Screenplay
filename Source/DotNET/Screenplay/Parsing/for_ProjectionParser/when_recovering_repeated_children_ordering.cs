// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing.for_ProjectionParser;

public class when_recovering_repeated_children_ordering : Specification
{
    const string Source =
        """
        projection Totals
          children items identified by id
            insert first
              ignored directive
            insert first
            sort by name descending
            sort by name sideways
            from Added
              total = amount
        """;

    ParserContext _context;
    ProjectionSyntax _projection;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _projection = ProjectionParser.Parse(_context, _context.Reader.TakeSignificant());

    [Fact] void should_keep_the_first_ordering() => Children.Ordering!.Placement.ShouldEqual(ChildrenPlacement.Prepend);
    [Fact] void should_keep_the_first_ordering_location() => Children.Ordering!.Location.ShouldEqual(new SourceLocation(3, 5));
    [Fact] void should_continue_after_the_orderings() => Children.Blocks.OfType<FromSyntax>().Single().Mappings.OfType<SetMappingSyntax>().Single().Property.ShouldEqual("total");

    [Fact] void should_distinguish_identical_and_conflicting_orderings() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(
        [
            "Children ordering 'insert first' takes no indented lines",
            "Children 'children items identified by id' declares its ordering twice - a children block takes one 'insert first', 'insert last' or 'sort by' line",
            "Children 'children items identified by id' declares conflicting orderings - a children block takes one 'insert first', 'insert last' or 'sort by' line",
            "Invalid children ordering 'sort by name sideways' - expected 'insert first', 'insert last' or 'sort by <property> [ascending|descending]'"
        ]);

    [Fact] void should_report_at_each_ordering_not_its_skipped_child() => _context.Diagnostics.Select(diagnostic => diagnostic.Location.Line).ShouldEqual([3, 5, 6, 7]);

    ChildrenSyntax Children => _projection.Blocks.OfType<ChildrenSyntax>().Single();
}
