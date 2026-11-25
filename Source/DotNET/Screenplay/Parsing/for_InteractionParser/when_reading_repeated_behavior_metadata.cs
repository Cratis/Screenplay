// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_InteractionParser;

public class when_reading_repeated_behavior_metadata : Specification
{
    const string Source = """
        behavior Sample
          description "First"
          description "Second"
          order 1
          order nope
          order 2
          file First.cs
          file Last.cs
          parameter
          parameter count Integer
          on click
            navigate back
        """;

    ParserContext _context;
    BehaviorSyntax _behavior;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _behavior = InteractionParser.ParseBehavior(_context, _context.Reader.TakeSignificant());

    [Fact] void should_keep_the_first_description() => _behavior.Description.ShouldEqual("First");
    [Fact] void should_keep_the_first_description_location() => _behavior.DirectiveLocations["description"].ShouldEqual(new SourceLocation(2, 3));
    [Fact] void should_keep_the_last_valid_order() => _behavior.Order.ShouldEqual(2);
    [Fact] void should_keep_the_last_valid_order_location() => _behavior.DirectiveLocations["order"].ShouldEqual(new SourceLocation(6, 3));
    [Fact] void should_keep_the_replaced_order_location() => _behavior.DirectiveLocations["omitted:order:4"].ShouldEqual(new SourceLocation(4, 3));
    [Fact] void should_keep_the_replaced_file_location() => _behavior.DirectiveLocations["omitted:file:7"].ShouldEqual(new SourceLocation(7, 3));
    [Fact] void should_keep_the_last_file_location() => _behavior.File!.Location.ShouldEqual(new SourceLocation(8, 3));
    [Fact] void should_omit_the_invalid_parameter() => _behavior.Parameters.Single().Name.ShouldEqual("count");
    [Fact] void should_keep_the_binding() => _behavior.Bindings.Single().Actions.Single().ShouldBeOfExactType<NavigateBackActionSyntax>();

    [Fact]
    void should_keep_the_diagnostic_order() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.DuplicateDescription, DiagnosticCodes.InvalidBehaviorOrder, DiagnosticCodes.InvalidBehaviorParameter]);

    [Fact]
    void should_keep_the_diagnostic_messages() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(
        ["behavior 'Sample' already declares a description - at most one is allowed", "Invalid order 'order nope' - expected 'order <number>'", "Invalid parameter declaration 'parameter' - expected 'parameter <name> [<Type>]'"]);

    [Fact]
    void should_keep_the_diagnostic_locations() => _context.Diagnostics.Select(diagnostic => diagnostic.Location).ShouldEqual(
        [new SourceLocation(3, 3), new SourceLocation(5, 3), new SourceLocation(9, 3)]);
}
