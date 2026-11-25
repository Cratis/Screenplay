// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_InteractionParser;

public class when_recovering_an_invalid_binding_trigger : Specification
{
    ParserContext _context;
    BehaviorSyntax? _behavior;

    void Establish() => _context = new(new(SourceLineSplitter.Split("on click extra\n  dance\non leave")));

    void Because() => _behavior = InteractionParser.ParseInlineBehavior(_context, _context.Reader.TakeSignificant());

    [Fact] void should_omit_the_invalid_behavior() => _behavior.ShouldBeNull();
    [Fact] void should_skip_its_children() => _context.Diagnostics.Count.ShouldEqual(1);
    [Fact] void should_leave_the_next_binding_unconsumed() => _context.Reader.PeekSignificant()!.Content.ShouldEqual("on leave");
    [Fact] void should_keep_the_diagnostic_code() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidInteractionTrigger);
    [Fact] void should_keep_the_diagnostic_message() => _context.Diagnostics.Single().Message.ShouldEqual("Invalid interaction trigger 'on click extra' - expected one of click, double click, select, submit, change, load, unload, enter, leave, 'event <Event>', 'interval <n> <unit>', or a declared application trigger name");
    [Fact] void should_keep_the_diagnostic_severity() => _context.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_keep_the_diagnostic_location() => _context.Diagnostics.Single().Location.ShouldEqual(new SourceLocation(1, 1));
}
