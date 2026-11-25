// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_InteractionParser;

public class when_recovering_an_invalid_behavior : Specification
{
    const string Source = """
        behavior
          unknown directive
            on click
              execute Ignored
        behavior Next
        """;

    ParserContext _context;
    BehaviorSyntax _behavior;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source, path: "behaviors.play")));

    void Because() => _behavior = InteractionParser.ParseBehavior(_context, _context.Reader.TakeSignificant());

    [Fact] void should_use_the_recovery_name() => _behavior.Name.ShouldEqual(string.Empty);
    [Fact] void should_skip_the_unknown_directives_children() => _behavior.Bindings.ShouldBeEmpty();
    [Fact] void should_keep_the_behavior_location() => _behavior.Location.ShouldEqual(new SourceLocation(1, 1, "behaviors.play"));
    [Fact] void should_leave_the_next_behavior_unconsumed() => _context.Reader.PeekSignificant()!.Content.ShouldEqual("behavior Next");

    [Fact]
    void should_keep_the_diagnostic_order() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.InvalidBehaviorDeclaration, DiagnosticCodes.UnknownBehaviorDirective, DiagnosticCodes.BehaviorWithoutBindings]);

    [Fact]
    void should_keep_the_diagnostic_messages() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(
        ["Invalid behavior declaration 'behavior' - expected 'behavior <Name>'", "Unexpected 'unknown directive' in behavior body - expected description, parameter, order or 'on <trigger>'", "Behavior '' declares no bindings - nothing it is attached to will ever run anything"]);

    [Fact]
    void should_keep_the_diagnostic_severities() => _context.Diagnostics.Select(diagnostic => diagnostic.Severity).ShouldEqual(
        [DiagnosticSeverity.Error, DiagnosticSeverity.Error, DiagnosticSeverity.Warning]);

    [Fact]
    void should_keep_the_diagnostic_locations() => _context.Diagnostics.Select(diagnostic => diagnostic.Location).ShouldEqual(
        [new SourceLocation(1, 1, "behaviors.play"), new SourceLocation(2, 3, "behaviors.play"), new SourceLocation(1, 1, "behaviors.play")]);
}
