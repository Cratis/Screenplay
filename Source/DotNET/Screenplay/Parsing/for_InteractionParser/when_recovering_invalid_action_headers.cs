// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_InteractionParser;

public class when_recovering_invalid_action_headers : Specification
{
    const string Source = """
        on click
          execute
            execute Ignored
          navigate sideways
          open
          close
          refresh
          set
          notify
          confirm
          raise
          dance
            execute IgnoredToo
          navigate back
        on leave
        """;

    ParserContext _context;
    BehaviorSyntax _behavior;

    void Establish() => _context = new(new(SourceLineSplitter.Split(Source, path: "actions.play")));

    void Because() => _behavior = InteractionParser.ParseInlineBehavior(_context, _context.Reader.TakeSignificant())!;

    [Fact] void should_keep_only_the_valid_sibling() => _behavior.Bindings.Single().Actions.Single().ShouldBeOfExactType<NavigateBackActionSyntax>();
    [Fact] void should_keep_the_valid_actions_location() => _behavior.Bindings.Single().Actions.Single().Location.ShouldEqual(new SourceLocation(14, 3, "actions.play"));
    [Fact] void should_leave_the_next_binding_unconsumed() => _context.Reader.PeekSignificant()!.Content.ShouldEqual("on leave");
    [Fact] void should_report_errors_only() => _context.Diagnostics.Select(diagnostic => diagnostic.Severity).Distinct().ShouldContainOnly(DiagnosticSeverity.Error);

    [Fact]
    void should_keep_the_diagnostic_order() => _context.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        [DiagnosticCodes.InvalidExecuteAction, DiagnosticCodes.InvalidNavigateAction, DiagnosticCodes.InvalidOpenDialogAction, DiagnosticCodes.InvalidOpenDialogAction, DiagnosticCodes.InvalidRefreshAction, DiagnosticCodes.InvalidSetAction, DiagnosticCodes.InvalidNotifyAction, DiagnosticCodes.InvalidConfirmAction, DiagnosticCodes.InvalidRaiseAction, DiagnosticCodes.UnknownInteractionAction]);

    [Fact]
    void should_keep_the_diagnostic_locations() => _context.Diagnostics.Select(diagnostic => diagnostic.Location).ShouldEqual(
        new[] { 2, 4, 5, 6, 7, 8, 9, 10, 11, 12 }.Select(line => new SourceLocation(line, 3, "actions.play")));

    [Fact]
    void should_keep_the_diagnostic_messages() => _context.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldEqual(
        ["Invalid execute action 'execute' - expected 'execute <Command>'", "Invalid navigate action 'navigate sideways' - expected 'navigate to <Screen>' or 'navigate back'", "Invalid open dialog action 'open' - expected 'open dialog <DialogTemplate>'", "Invalid close action 'close' - expected 'close dialog'", "Invalid refresh action 'refresh' - expected 'refresh <Query>'", "Invalid set action 'set' - expected 'set <target> to <value>'", "Invalid notify action 'notify' - expected 'notify <info|warning|error> \"<text>\"'", "Invalid confirm action 'confirm' - expected 'confirm \"<text>\"'", "Invalid raise action 'raise' - expected 'raise <ApplicationTrigger>'", "Unexpected 'dance' where an action was expected - expected execute, navigate, open dialog, close dialog, refresh, set, notify, confirm or raise"]);
}
