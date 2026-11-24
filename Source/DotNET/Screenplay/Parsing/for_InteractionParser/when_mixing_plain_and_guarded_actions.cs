// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_InteractionParser;

public class when_mixing_plain_and_guarded_actions : Specification
{
    const string Source = """
        on submit
          where $context.ready
          navigate back
          when item.status == "open"
            execute First
          when item.status == "closed"
            execute Second
          close dialog
          otherwise
            notify info "Done"
          refresh Items
        """;

    ParserContext _context;
    InteractionBindingSyntax _binding;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source)));

    void Because() => _binding = InteractionParser.ParseInlineBehavior(_context, _context.Reader.TakeSignificant())!.Bindings.Single();

    [Fact] void should_retain_the_plain_actions_in_order() => _binding.Actions.Select(action => action.GetType()).ShouldEqual([typeof(NavigateBackActionSyntax), typeof(CloseDialogActionSyntax), typeof(RefreshQueryActionSyntax)]);
    [Fact] void should_retain_the_alternatives_in_order() => _binding.Alternatives.Select(alternative => ((ExecuteCommandActionSyntax)alternative.Actions.Single()).Command).ShouldEqual(["First", "Second"]);
    [Fact] void should_retain_the_fallback_action() => _binding.Otherwise!.Actions.Single().ShouldBeOfExactType<NotifyActionSyntax>();

    [Fact]
    void should_report_the_unsupported_trigger_before_mixing() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.UnsupportedInteractionAlternatives, DiagnosticCodes.MixedInteractionAlternatives]);

    [Fact]
    void should_keep_the_diagnostic_messages() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(
        ["Interaction alternatives are only supported on click, double click, select and change", "Interaction alternatives cannot mix with plain actions or 'where'"]);

    [Fact]
    void should_keep_the_diagnostic_locations() => _context.Diagnostics.Select(diagnostic => diagnostic.Location).ShouldEqual([new SourceLocation(1, 1), new SourceLocation(1, 1)]);
}
