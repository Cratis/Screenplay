// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing.for_ProjectionParser;

public class when_recovering_from_an_invalid_header : Specification
{
    ParserContext _context;
    ProjectionSyntax _projection;

    void Establish() => _context = new(new(SourceLineSplitter.Split("projection =>\n  from Ignored\n    id = id\nprojection Next\n", path: "projections.play")));

    void Because() => _projection = ProjectionParser.Parse(_context, _context.Reader.TakeSignificant());

    [Fact] void should_report_the_header() => _context.Diagnostics.Single().Message.ShouldEqual("Invalid projection declaration 'projection =>' - expected 'projection <Name> [=> <ReadModel>]'");
    [Fact] void should_keep_the_diagnostic_code() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidProjectionDeclaration);
    [Fact] void should_keep_the_header_location() => _projection.Location.ShouldEqual(new SourceLocation(1, 1, "projections.play"));
    [Fact] void should_return_the_recovery_name() => _projection.Name.ShouldEqual("projection");
    [Fact] void should_return_no_blocks() => _projection.Blocks.ShouldBeEmpty();
    [Fact] void should_leave_the_next_declaration_unconsumed() => _context.Reader.PeekSignificant()!.Content.ShouldEqual("projection Next");
}
