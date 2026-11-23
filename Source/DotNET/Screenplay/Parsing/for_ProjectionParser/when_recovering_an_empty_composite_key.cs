// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing.for_ProjectionParser;

public class when_recovering_an_empty_composite_key : Specification
{
    const string Source =
        """
        projection Totals
          from Added
            key {
              broken part
            }
            total = amount
        """;

    ParserContext _context;
    ProjectionSyntax _projection;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _projection = ProjectionParser.Parse(_context, _context.Reader.TakeSignificant());

    [Fact] void should_keep_an_untyped_composite_key() => Key.Type.ShouldEqual(string.Empty);
    [Fact] void should_keep_no_parts() => Key.Parts.ShouldBeEmpty();
    [Fact] void should_keep_the_following_mapping() => From.Mappings.OfType<SetMappingSyntax>().Single().Property.ShouldEqual("total");

    [Fact] void should_report_the_empty_key_after_its_invalid_part() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.RedundantCompositeKeyBraces, DiagnosticCodes.InvalidCompositeKeyPart, DiagnosticCodes.EmptyCompositeKey]);

    [Fact] void should_report_the_empty_key_at_its_header() => _context.Diagnostics.Last().Location.ShouldEqual(new SourceLocation(3, 5));

    FromSyntax From => _projection.Blocks.OfType<FromSyntax>().Single();
    CompositeKeySyntax Key => (CompositeKeySyntax)From.Key!;
}
