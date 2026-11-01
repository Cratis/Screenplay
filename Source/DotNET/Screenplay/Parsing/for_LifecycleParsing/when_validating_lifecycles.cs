// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Parsing.for_LifecycleParsing;

public class when_validating_lifecycles : Specification
{
    const string _valid = a_lifecycle_source.Source;

    static IEnumerable<string> Codes(string source) => new ScreenplayCompiler().Compile(source).Diagnostics.Select(diagnostic => diagnostic.Code).Distinct();

    static string Replace(string original, string replacement) => _valid.Replace(original, replacement, StringComparison.Ordinal);

    [Fact] void should_accept_the_valid_model() => new ScreenplayCompiler().Compile(_valid).Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_report_no_initial_state() => Codes(Replace("state Draft initial", "state Draft")).ShouldContain(DiagnosticCodes.StateMachineInitialState);
    [Fact] void should_report_two_initial_states() => Codes(Replace("state Paid final", "state Paid initial")).ShouldContain(DiagnosticCodes.StateMachineInitialState);
    [Fact] void should_report_an_unreachable_state() => Codes(Replace("    on InvoiceMarkedOverdue when overdue == true -> Overdue\n", string.Empty)).ShouldContain(DiagnosticCodes.UnreachableState);
    [Fact] void should_report_a_state_with_no_way_out() => Codes(Replace("  state Overdue\n    on InvoicePaid -> Paid\n", "  state Overdue\n")).ShouldContain(DiagnosticCodes.StateWithNoWayOut);
    [Fact] void should_report_a_final_state_with_transitions() => Codes(Replace("  state Paid final\n", "  state Paid final\n    on InvoiceSent -> Draft\n")).ShouldContain(DiagnosticCodes.FinalStateWithTransitions);
    [Fact] void should_report_a_transition_to_an_unknown_state() => Codes(Replace("on InvoiceSent -> Sent", "on InvoiceSent -> Shipped")).ShouldContain(DiagnosticCodes.UnknownState);
    [Fact] void should_report_an_unknown_event() => Codes(Replace("on InvoiceSent -> Sent", "on InvoiceMissing -> Sent")).ShouldContain(DiagnosticCodes.UnknownStateMachineReference);
    [Fact] void should_report_an_unknown_read_model() => Codes(Replace("follows Invoice\n", "follows Missing\n")).ShouldContain(DiagnosticCodes.UnknownStateMachineReference);
    [Fact] void should_report_an_unknown_condition_property() => Codes(Replace("when overdue == true -> Overdue", "when missing == true -> Overdue")).ShouldContain(DiagnosticCodes.UnknownConditionProperty);
    [Fact] void should_report_ambiguous_transitions() => Codes(Replace("on InvoicePaid -> Paid\n  state Overdue", "on InvoicePaid -> Paid\n    on InvoicePaid -> Paid\n  state Overdue")).ShouldContain(DiagnosticCodes.AmbiguousStateTransitions);
    [Fact] void should_report_a_workflow_without_a_trigger() => Codes(Replace("trigger Started manual\n    -> ChargeLateFee\n", string.Empty).Replace("trigger when InvoiceLifecycle enters Overdue\n    -> ChargeLateFee\n", string.Empty).Replace("trigger Daily every 1 days\n    -> ChargeLateFee\n", string.Empty).Replace("trigger Weekly at 08:00 on Monday\n    -> ChargeLateFee\n", string.Empty).Replace("trigger Flagged on InvoiceFlagged\n    -> ChargeLateFee\n", string.Empty)).ShouldContain(DiagnosticCodes.WorkflowWithoutTrigger);
    [Fact] void should_report_an_unreachable_node() => Codes(Replace("  action Done sends TagInvoice\n", "  action Done sends TagInvoice\n  action Orphan sends TagInvoice\n")).ShouldContain(DiagnosticCodes.UnreachableWorkflowNode);
    [Fact] void should_report_an_edge_to_an_unknown_node() => Codes(Replace("failed -> Approve", "failed -> Nowhere")).ShouldContain(DiagnosticCodes.UnknownWorkflowNode);
    [Fact] void should_report_an_outlet_the_kind_does_not_have() => Codes(Replace("failed -> Approve", "rejected -> Approve")).ShouldContain(DiagnosticCodes.UnknownWorkflowOutlet);
    [Fact] void should_report_a_decision_with_one_outlet() => Codes(Replace("    \"settled\" -> Done\n", string.Empty)).ShouldContain(DiagnosticCodes.WorkflowDecisionOutletWithoutTarget);
    [Fact] void should_warn_about_a_wait_with_no_expiry() => Codes(Replace("wait Await for InvoicePaid expires after 7 days", "wait Await for InvoicePaid")).ShouldContain(DiagnosticCodes.WorkflowWithoutExpiry);
    [Fact] void should_inform_about_a_join_with_one_branch() => Codes(_valid).ShouldContain(DiagnosticCodes.WorkflowJoinWithOneBranch);
    [Fact] void should_report_an_unknown_command() => Codes(Replace("sends WriteOff\n", "sends Missing\n")).ShouldContain(DiagnosticCodes.UnknownWorkflowReference);
    [Fact] void should_report_an_unknown_approver() => Codes(Replace("approval Approve by Controller", "approval Approve by Nobody")).ShouldContain(DiagnosticCodes.UnknownWorkflowReference);
    [Fact] void should_report_an_unknown_state_in_a_trigger() => Codes(Replace("enters Overdue", "enters Missing")).ShouldContain(DiagnosticCodes.UnknownState);
    [Fact] void should_report_a_malformed_state_machine() => Codes(Replace("follows Invoice\n", "follows\n")).ShouldContain(DiagnosticCodes.InvalidStateMachineDeclaration);
    [Fact] void should_report_a_malformed_workflow() => Codes(Replace("wait Pause for 2 days", "wait Pause for 0 days")).ShouldContain(DiagnosticCodes.InvalidWorkflowDeclaration);
}
