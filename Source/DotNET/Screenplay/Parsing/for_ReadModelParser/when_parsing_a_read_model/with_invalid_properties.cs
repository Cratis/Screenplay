// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ReadModelParser.when_parsing_a_read_model;

public class with_invalid_properties : Specification
{
    const string Source = """
        readmodel Row
          id String
            nested String identifier key
          missingType
          count Int
        """;

    ParserContext _context = null!;
    ReadModelSyntax _model = null!;

    PropertySyntax Nested => _model.Properties.Single(property => string.Equals(property.Name, "nested", StringComparison.Ordinal));

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _model = ReadModelParser.Parse(_context, _context.Reader.TakeSignificant());

    [Fact] void should_omit_the_malformed_property_without_reordering_the_others() => _model.Properties.Select(property => property.Name).ShouldEqual("id", "nested", "count");
    [Fact] void should_clear_the_nested_key() => Nested.IsKey.ShouldBeFalse();
    [Fact] void should_clear_the_identifier() => Nested.IsIdentifier.ShouldBeFalse();
    [Fact] void should_name_the_identifier_property_in_the_diagnostic() => _context.Diagnostics.Single(diagnostic => string.Equals(diagnostic.Code, DiagnosticCodes.IdentifierOutsideCommand, StringComparison.Ordinal)).Message.ShouldContain("'nested'");
    [Fact] void should_show_the_expected_property_syntax() => _context.Diagnostics.Single(diagnostic => string.Equals(diagnostic.Code, DiagnosticCodes.InvalidPropertyDeclaration, StringComparison.Ordinal)).Message.ShouldContain("'<name> <Type>'");
    [Fact] void should_report_modifier_conflicts_before_the_invalid_declaration() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        DiagnosticCodes.InvalidReadModelKey,
        DiagnosticCodes.InvalidReadModelKey,
        DiagnosticCodes.IdentifierOutsideCommand,
        DiagnosticCodes.InvalidPropertyDeclaration);
    [Fact] void should_report_each_diagnostic_at_its_property() => _context.Diagnostics.Select(diagnostic => diagnostic.Location).ShouldEqual(
        new SourceLocation(3, 5),
        new SourceLocation(3, 5),
        new SourceLocation(3, 5),
        new SourceLocation(4, 3));
}
