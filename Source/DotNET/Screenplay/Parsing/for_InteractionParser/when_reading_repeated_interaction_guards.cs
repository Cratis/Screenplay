// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_InteractionParser;

public class when_reading_repeated_interaction_guards : Specification
{
    const string Source = """
        on click
          where item.status == "open"
          where $context.ready == true
          where item.status == "closed"
          navigate back
        """;

    ParserContext _context;
    InteractionBindingSyntax _binding;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _binding = InteractionParser.ParseInlineBehavior(_context, _context.Reader.TakeSignificant())!.Bindings.Single();

    [Fact] void should_keep_the_first_guard() => _binding.Condition.ShouldEqual("item.status == \"open\"");
    [Fact] void should_keep_the_first_guard_location() => _binding.DirectiveLocations["where"].ShouldEqual(new SourceLocation(2, 3));
    [Fact] void should_keep_only_the_accepted_guard_location() => _binding.DirectiveLocations.Count.ShouldEqual(1);
    [Fact] void should_keep_the_action() => _binding.Actions.Single().ShouldBeOfExactType<NavigateBackActionSyntax>();

    [Fact]
    void should_report_repeated_guards_before_deprecation() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.RepeatedInteractionCondition, DiagnosticCodes.RepeatedInteractionCondition, DiagnosticCodes.LegacyInteractionWhere]);

    [Fact]
    void should_keep_the_diagnostic_severities() => _context.Diagnostics.Select(diagnostic => diagnostic.Severity).ShouldEqual(
        [DiagnosticSeverity.Error, DiagnosticSeverity.Error, DiagnosticSeverity.Warning]);

    [Fact]
    void should_keep_the_diagnostic_locations() => _context.Diagnostics.Select(diagnostic => diagnostic.Location.Line).ShouldEqual([3, 4, 2]);

    [Fact]
    void should_keep_the_repeated_guard_message() => _context.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedInteractionCondition).Select(diagnostic => diagnostic.Message).Distinct().ShouldContainOnly("The binding already declares a 'where' guard - a binding has at most one");
}
