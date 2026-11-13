// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public partial class ScreenplayPrinter
{
    static string NodeText(WorkflowNodeSyntax node) => node.NodeKind switch
    {
        WorkflowNodeKind.Trigger => $"trigger{TriggerName(node)} {TriggerText(node)}",
        WorkflowNodeKind.Action => $"action {node.Name} invokes {node.Command}{(node.Attempts is { } attempts ? $" attempts {attempts}" : string.Empty)}{(node.Timeout is { } timeout ? $" timeout {DurationText(timeout)}" : string.Empty)}{(node.RetryAfter is { } retry ? $" retry after {DurationText(retry)}" : string.Empty)}{(node.ExpiresAfter is { } actionExpiry ? $" expires after {DurationText(actionExpiry)}" : string.Empty)}",
        WorkflowNodeKind.Compute => $"compute {node.Name}",
        WorkflowNodeKind.Decide => $"decide {node.Name} when {ScreenplaySyntaxText.Condition(node.Condition!)}",
        WorkflowNodeKind.Wait => node.Duration is { } duration
            ? $"wait {node.Name} for {DurationText(duration)}"
            : $"wait {node.Name} for {node.Event}{(node.ExpiresAfter is { } expires ? $" expires after {DurationText(expires)}" : string.Empty)}",
        WorkflowNodeKind.Approval => $"approval {node.Name} by {node.By}{(node.Via is null ? string.Empty : $" via {node.Via}")}{(node.ExpiresAfter is { } expiry ? $" expires after {DurationText(expiry)}" : string.Empty)}",
        WorkflowNodeKind.ForEach => $"foreach {node.Name} over {node.Items}{(node.Parallel is { } parallel ? $" parallel {parallel}" : string.Empty)}",
        _ => $"join {node.Name} {JoinText(node)}"
    };

    static string TriggerName(WorkflowNodeSyntax node) => ImplicitTriggerNameRegex().IsMatch(node.Name) ? string.Empty : $" {node.Name}";

    static string TriggerText(WorkflowNodeSyntax node) => node.TriggerKind switch
    {
        WorkflowTriggerKind.Event => $"on {node.Event}",
        WorkflowTriggerKind.StateEntered => $"when {node.StateMachine} enters {node.State}",
        WorkflowTriggerKind.Schedule => ScreenplaySyntaxText.TriggerSource(node.Schedule!),
        _ => "manual"
    };

    static string JoinText(WorkflowNodeSyntax node) => node.JoinMode switch
    {
        WorkflowJoinMode.Any => "any",
        WorkflowJoinMode.AtLeast => $"at least {node.JoinCount}",
        _ => "all"
    };

    static string DurationText(WorkflowDurationSyntax duration) => $"{duration.Amount} {ScreenplaySyntaxText.IntervalUnitText(duration.Amount, duration.Unit)}";

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex SimpleOutletRegex();

    [GeneratedRegex(@"^Trigger\d*$", RegexOptions.NonBacktracking)]
    private static partial Regex ImplicitTriggerNameRegex();

    void WriteStateEntry(ScreenplayWriter writer, StateEntrySyntax entry)
    {
        switch (entry.EntryKind)
        {
            case StateEntryKind.Body:
                writer.Line("entering", entry);
                using (writer.Indent())
                {
                    WriteFile(writer, entry.File);
                    if (entry.Code is not null) WriteCodeBlock(writer, entry.Code);
                }

                break;
            case StateEntryKind.Invoke:
                writer.Line($"entering invokes {entry.Target}", entry);
                using (writer.Indent()) WriteMappings(writer, entry.Mappings, ReservedWords.MappingBlock);
                break;
            default:
                writer.Line($"entering {(entry.EntryKind == StateEntryKind.Send ? "invokes" : "starts")} {entry.Target}", entry);
                break;
        }
    }

    void WriteStateMachine(ScreenplayWriter writer, StateMachineSyntax machine)
    {
        using var anchor = writer.Anchor(machine);
        writer.Line($"statemachine {machine.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, machine.Description, machine);
            writer.DirectiveLine($"follows {machine.Follows}", machine, "follows");
            foreach (var state in machine.States)
            {
                using var stateAnchor = writer.Anchor(state);
                writer.Line($"state {state.Name}{(state.IsInitial ? " initial" : string.Empty)}{(state.IsFinal ? " final" : string.Empty)}", state);
                using (writer.Indent())
                {
                    foreach (var entry in state.Entering) WriteStateEntry(writer, entry);
                    foreach (var transition in state.Transitions)
                    {
                        var when = transition.When is null ? string.Empty : $" when {ScreenplaySyntaxText.Condition(transition.When)}";
                        writer.Line($"on {transition.Event}{when} -> {transition.Target}", transition);
                    }
                }
            }

            foreach (var specification in machine.Specifications) WriteLifecycleSpecification(writer, specification);
        }
    }

    void WriteWorkflow(ScreenplayWriter writer, WorkflowSyntax workflow)
    {
        using var anchor = writer.Anchor(workflow);
        writer.Line($"workflow {workflow.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, workflow.Description, workflow);
            writer.DirectiveLine($"for {workflow.Subject}", workflow, "for");
            foreach (var node in workflow.Nodes)
            {
                using var nodeAnchor = writer.Anchor(node);
                writer.Line(NodeText(node), node);
                using (writer.Indent())
                {
                    WriteMappings(writer, node.Mappings, ReservedWords.MappingBlock);
                    if (node.Returns.Any())
                    {
                        writer.Line("returns");
                        using (writer.Indent()) WriteProperties(writer, node.Returns, _emptyKeywords);
                    }

                    WriteFile(writer, node.File);
                    if (node.Code is not null) WriteCodeBlock(writer, node.Code);
                    foreach (var edge in node.Edges)
                    {
                        var outlet = node.NodeKind == WorkflowNodeKind.Decide || !SimpleOutletRegex().IsMatch(edge.Outlet) ? $"\"{edge.Outlet}\"" : edge.Outlet;
                        if (node.NodeKind == WorkflowNodeKind.Trigger && edge.Outlet == "next" && edge.Target.Length > 0) writer.Line($"-> {edge.Target}", edge);
                        else writer.Line(edge.Target.Length == 0 ? outlet : $"{outlet} -> {edge.Target}", edge);
                    }
                }
            }

            foreach (var specification in workflow.Specifications) WriteLifecycleSpecification(writer, specification);
        }
    }

    void WriteLifecycleSpecification(ScreenplayWriter writer, LifecycleSpecificationSyntax specification)
    {
        using var anchor = writer.Anchor(specification);
        writer.Line($"specification {specification.Name}", specification);
        using (writer.Indent())
        {
            WriteDescription(writer, specification.Description, specification);
            foreach (var given in specification.Given) WriteSpecificationEvent(writer, "given", given);
            if (specification.WhenAppended is not null) WriteSpecificationEvent(writer, "when append", specification.WhenAppended);
            if (specification.WhenStarted is not null) writer.Line($"when start {writer.Expression(specification.WhenStarted)}");
            foreach (var outcome in specification.Outcomes) writer.Line($"outcome {outcome.Node} {outcome.Outlet}", outcome);
            foreach (var expectation in specification.Then)
            {
                var text = expectation.Expectation switch
                {
                    LifecycleExpectationKind.State => $"then is {expectation.Target}",
                    LifecycleExpectationKind.Sends => $"then invokes {expectation.Target}",
                    LifecycleExpectationKind.Starts => $"then starts {expectation.Target}",
                    LifecycleExpectationKind.Fails => $"then fails at {expectation.Target}",
                    LifecycleExpectationKind.Waits => $"then waits at {expectation.Target}",
                    _ => "then completes"
                };
                writer.Line(text, expectation);
            }
        }
    }
}
