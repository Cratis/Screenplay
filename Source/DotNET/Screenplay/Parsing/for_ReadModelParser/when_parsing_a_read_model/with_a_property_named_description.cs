// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ReadModelParser.when_parsing_a_read_model;

public class with_a_property_named_description : Specification
{
    const string Source = """
        readmodel Row
          description String
          description "RowMetadata"
          status String
        """;

    ParserContext _context = null!;
    ReadModelSyntax _model = null!;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _model = ReadModelParser.Parse(_context, _context.Reader.TakeSignificant());

    [Fact] void should_keep_the_properties_in_source_order() => _model.Properties.Select(property => property.Name).ShouldEqual("description", "status");
    [Fact] void should_use_the_directive_as_the_description() => _model.Description.ShouldEqual("RowMetadata");
    [Fact] void should_record_the_directive_location_not_the_property_location() => _model.DirectiveLocations["description"].ShouldEqual(new SourceLocation(3, 3));
    [Fact] void should_accept_both_without_diagnostics() => _context.Diagnostics.ShouldBeEmpty();
}
