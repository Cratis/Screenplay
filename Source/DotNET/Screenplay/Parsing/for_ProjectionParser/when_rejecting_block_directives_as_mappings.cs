// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing.for_ProjectionParser;

public class when_rejecting_block_directives_as_mappings : Specification
{
    const string Source =
        """
        projection Totals
          from Added
            remove via join on items
            remove via
            clear with
            remove @via.$eventContent.id
            total = amount
        """;

    ParserContext _context;
    ProjectionSyntax _projection;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _projection = ProjectionParser.Parse(_context, _context.Reader.TakeSignificant());

    [Fact] void should_reject_only_the_unescaped_directives() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.InvalidProjectionMapping, DiagnosticCodes.InvalidProjectionMapping, DiagnosticCodes.InvalidProjectionMapping]);

    [Fact] void should_keep_the_directive_hints() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(
        [
            "Invalid mapping 'remove via join on items' - 'remove via' is a block directive of the projection or children body, not a mapping line",
            "Invalid mapping 'remove via' - 'remove via' is a block directive and needs an event type; to remove a property named 'via', write 'remove @via'",
            "Invalid mapping 'clear with' - 'clear with' is a block directive and needs an event type; to clear a property named 'with', write 'clear @with'"
        ]);

    [Fact] void should_keep_the_diagnostic_subjects() => _context.Diagnostics.Select(diagnostic => diagnostic.Arguments["text"]).ShouldEqual(
        [DiagnosticArgument.Text("remove via join on items"), DiagnosticArgument.Text("remove via"), DiagnosticArgument.Text("clear with")]);

    [Fact] void should_keep_the_escaped_property() => From.Mappings.OfType<RemoveKeyMappingSyntax>().Single().Property.ShouldEqual("via.$eventContent.id");
    [Fact] void should_keep_the_following_assignment() => From.Mappings.OfType<SetMappingSyntax>().Single().Property.ShouldEqual("total");
    [Fact] void should_keep_the_mapping_location() => From.Mappings.OfType<SetMappingSyntax>().Single().Location.ShouldEqual(new SourceLocation(7, 5));

    FromSyntax From => _projection.Blocks.OfType<FromSyntax>().Single();
}
