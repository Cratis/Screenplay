// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_InteractionParser;

public class when_an_interaction_has_only_a_fallback : Specification
{
    ParserContext _context;
    InteractionBindingSyntax _binding;

    void Establish() => _context = new(new(SourceLineSplitter.Split("on click\n  otherwise\n    navigate back\non leave")));

    void Because() => _binding = InteractionParser.ParseInlineBehavior(_context, _context.Reader.TakeSignificant())!.Bindings.Single();

    [Fact] void should_keep_the_fallback() => _binding.Otherwise!.Actions.Single().ShouldBeOfExactType<NavigateBackActionSyntax>();
    [Fact] void should_leave_the_next_binding_unconsumed() => _context.Reader.PeekSignificant()!.Content.ShouldEqual("on leave");

    [Fact] void should_report_only_missing_alternatives() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.GuardedActionWithoutAlternatives);
    [Fact] void should_keep_the_diagnostic_message() => _context.Diagnostics.Single().Message.ShouldEqual("A guarded interaction requires at least one 'when' alternative");
    [Fact] void should_keep_the_diagnostic_location() => _context.Diagnostics.Single().Location.ShouldEqual(new SourceLocation(1, 1));
}
