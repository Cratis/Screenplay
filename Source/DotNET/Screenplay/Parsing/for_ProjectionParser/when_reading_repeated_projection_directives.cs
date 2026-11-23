// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing.for_ProjectionParser;

public class when_reading_repeated_projection_directives : Specification
{
    const string Source =
        """
        projection Work => WorkModel
          description "Work items"
          sequence first
          sequence second
          no automap
          automap
          key firstId
          key secondId
          variant Open
            enters on Created
          variant Open
            enters on Created
          from Changed
            id = id
        """;

    ParserContext _context;
    ProjectionSyntax _projection;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _projection = ProjectionParser.Parse(_context, _context.Reader.TakeSignificant());

    [Fact] void should_keep_the_description() => _projection.Description.ShouldEqual("Work items");
    [Fact] void should_keep_the_read_model() => _projection.ReadModel.ShouldEqual("WorkModel");
    [Fact] void should_keep_the_last_sequence() => _projection.Sequence.ShouldEqual("second");
    [Fact] void should_keep_the_last_sequence_location() => _projection.DirectiveLocations["sequence"].ShouldEqual(new SourceLocation(4, 3));
    [Fact] void should_keep_the_last_automap() => _projection.AutoMap.ShouldEqual(AutoMapMode.Enabled);
    [Fact] void should_keep_the_parsed_automap() => _projection.ParsedAutoMapMode.ShouldEqual(AutoMapMode.Enabled);
    [Fact] void should_keep_the_previous_automap_location() => _projection.DirectiveLocations["automap previous:Disabled:5"].ShouldEqual(new SourceLocation(5, 3));
    [Fact] void should_keep_the_last_key() => _projection.Key!.Location.ShouldEqual(new SourceLocation(8, 3));
    [Fact] void should_keep_both_variants() => _projection.Blocks.OfType<ProjectionVariantSyntax>().Count().ShouldEqual(2);

    [Fact] void should_report_directives_before_variant_validation() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.RepeatedProjectionAutoMap, DiagnosticCodes.UnusedProjectionKey, DiagnosticCodes.UnusedProjectionKey, DiagnosticCodes.DuplicateProjectionKey, DiagnosticCodes.DuplicateProjectionVariant, DiagnosticCodes.DuplicateVariantEnteringEvent]);

    [Fact] void should_report_at_the_repeated_declarations() => _context.Diagnostics.Select(diagnostic => diagnostic.Location.Line).ShouldEqual([6, 7, 8, 8, 11, 12]);
}
