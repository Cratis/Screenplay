// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ReadModelParser.when_parsing_a_read_model;

public class with_repeated_descriptions : Specification
{
    const string Source = """
        readmodel Row
          description not quoted
          description "First"
          description "Second"
        """;

    ParserContext _context = null!;
    ReadModelSyntax _model = null!;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _model = ReadModelParser.Parse(_context, _context.Reader.TakeSignificant());

    [Fact] void should_keep_the_first_valid_description() => _model.Description.ShouldEqual("First");
    [Fact] void should_keep_the_first_valid_description_location() => _model.DirectiveLocations["description"].ShouldEqual(new SourceLocation(3, 3));
    [Fact] void should_report_only_the_malformed_and_duplicate_descriptions_in_order() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(DiagnosticCodes.InvalidDescription, DiagnosticCodes.DuplicateDescription);
    [Fact] void should_name_the_read_model_in_the_duplicate_diagnostic() => _context.Diagnostics.Single(diagnostic => string.Equals(diagnostic.Code, DiagnosticCodes.DuplicateDescription, StringComparison.Ordinal)).Message.ShouldContain("'Row'");
}
