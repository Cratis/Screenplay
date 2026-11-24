// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_InteractionParser;

public class when_a_binding_has_no_actions : Specification
{
    ParserContext _context;
    BehaviorSyntax _behavior;

    void Establish() => _context = new(new(SourceLineSplitter.Split("on click\non leave")));

    void Because() => _behavior = InteractionParser.ParseInlineBehavior(_context, _context.Reader.TakeSignificant())!;

    [Fact] void should_keep_the_empty_binding() => _behavior.Bindings.Single().Actions.ShouldBeEmpty();
    [Fact] void should_leave_the_next_binding_unconsumed() => _context.Reader.PeekSignificant()!.Content.ShouldEqual("on leave");
    [Fact] void should_report_the_missing_actions() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InteractionBindingWithoutActions);
    [Fact] void should_keep_the_diagnostic_message() => _context.Diagnostics.Single().Message.ShouldEqual("'on click' declares no actions - a trigger with nothing to do is never what was meant");
    [Fact] void should_keep_the_diagnostic_severity() => _context.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_keep_the_diagnostic_location() => _context.Diagnostics.Single().Location.ShouldEqual(new SourceLocation(1, 1));
}
