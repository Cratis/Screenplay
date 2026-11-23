// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing.for_ProjectionParser;

public class when_recovering_composite_key_parts : Specification
{
    const string Source =
        """
        projection Totals
          from Added
            key Pair {
              broken part
              @group = `${id}`
              }
            }
            total = amount
        """;

    ParserContext _context;
    ProjectionSyntax _projection;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _projection = ProjectionParser.Parse(_context, _context.Reader.TakeSignificant());

    [Fact] void should_keep_the_composite_type() => Key.Type.ShouldEqual("Pair");
    [Fact] void should_discard_only_the_invalid_part() => Key.Parts.Single().Property.ShouldEqual("group");
    [Fact] void should_retain_the_forbidden_template() => Key.Parts.Single().Expression.ShouldBeOfExactType<TemplateExpressionSyntax>();
    [Fact] void should_keep_the_part_location() => Key.Parts.Single().Location.ShouldEqual(new SourceLocation(5, 7));
    [Fact] void should_consume_both_closing_braces() => From.Mappings.OfType<SetMappingSyntax>().Single().Property.ShouldEqual("total");
    [Fact] void should_consume_the_input() => _context.Reader.AtEnd.ShouldBeTrue();

    [Fact] void should_keep_diagnostic_order() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.RedundantCompositeKeyBraces, DiagnosticCodes.InvalidCompositeKeyPart, DiagnosticCodes.TemplateInCompositeKey]);

    [Fact] void should_keep_diagnostic_locations() => _context.Diagnostics.Select(diagnostic => diagnostic.Location).ShouldEqual(
        [new SourceLocation(3, 5), new SourceLocation(4, 7), new SourceLocation(5, 7)]);

    FromSyntax From => _projection.Blocks.OfType<FromSyntax>().Single();
    CompositeKeySyntax Key => (CompositeKeySyntax)From.Key!;
}
