// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Writes the ESM v36 members: state machines and workflows. Each is written only when the model holds it, so the bytes
/// of models before v36 do not change.
/// </summary>
public static partial class SemanticModelCanonicalJson
{
    static void WriteLifecycles(Utf8JsonWriter writer, SemanticApplication application, SemanticVersion version)
    {
        var hasMachines = !application.StateMachines.IsDefaultOrEmpty;
        var hasWorkflows = !application.Workflows.IsDefaultOrEmpty;
        if ((hasMachines || hasWorkflows) && version.Major < 36)
        {
            throw new InvalidSemanticContract("State machines and workflows require ESM v36 or later.");
        }

        if (hasMachines) WriteArray(writer, "stateMachines", application.StateMachines.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteStateMachine);
        if (hasWorkflows) WriteArray(writer, "workflows", application.Workflows.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteWorkflow);
    }

    static void WriteStateMachine(Utf8JsonWriter writer, SemanticStateMachine machine)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", machine.Name);
        writer.WriteString("readModel", machine.ReadModel.ToString());
        WriteArray(writer, "states", machine.States, WriteState);
        WriteArray(writer, "transitions", machine.Transitions, WriteStateTransition);
        WriteLifecycleSpecifications(writer, machine.Specifications);
        writer.WriteEndObject();
    }

    static void WriteState(Utf8JsonWriter writer, SemanticState state)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", state.Name);
        writer.WriteBoolean("initial", state.IsInitial);
        writer.WriteBoolean("final", state.IsFinal);
        WriteArray(writer, "entering", state.Entering, (output, entry) =>
        {
            output.WriteStartObject();
            if (entry.Kind == SemanticStateEntryKind.SendCommand)
            {
                output.WriteString("kind", "send");
                output.WriteString("command", entry.Command.ToString());
            }
            else
            {
                output.WriteString("kind", "start");
                CanonicalJson.WriteString(output, "workflow", entry.Workflow!);
            }

            output.WriteEndObject();
        });
        writer.WriteEndObject();
    }

    static void WriteStateTransition(Utf8JsonWriter writer, SemanticStateTransition transition)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "from", transition.From);
        writer.WriteString("event", transition.Event.ToString());
        if (transition.Guard is not null)
        {
            writer.WritePropertyName("guard");
            WriteCondition(writer, transition.Guard);
        }

        CanonicalJson.WriteString(writer, "to", transition.To);
        writer.WriteEndObject();
    }

    static void WriteWorkflow(Utf8JsonWriter writer, SemanticWorkflow workflow)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", workflow.Name);
        writer.WriteString("subject", workflow.Subject.ToString());
        WriteArray(writer, "nodes", workflow.Nodes, WriteWorkflowNode);
        WriteArray(writer, "edges", workflow.Edges, (output, edge) =>
        {
            output.WriteStartObject();
            CanonicalJson.WriteString(output, "from", edge.From);
            CanonicalJson.WriteString(output, "outlet", edge.Outlet);
            CanonicalJson.WriteString(output, "to", edge.To);
            output.WriteEndObject();
        });
        WriteLifecycleSpecifications(writer, workflow.Specifications);
        writer.WriteEndObject();
    }

    static void WriteLifecycleSpecifications(Utf8JsonWriter writer, System.Collections.Immutable.ImmutableArray<SemanticLifecycleSpecification> specifications)
    {
        if (specifications.IsDefaultOrEmpty) return;
        WriteArray(writer, "specifications", specifications, (output, specification) =>
        {
            output.WriteStartObject();
            CanonicalJson.WriteString(output, "name", specification.Name);
            WriteArray(output, "given", specification.Given, WriteSpecificationEvent);
            if (specification.WhenAppended is not null)
            {
                output.WritePropertyName("whenAppended");
                WriteSpecificationEvent(output, specification.WhenAppended);
            }

            if (specification.WhenStarted is not null)
            {
                output.WritePropertyName("whenStarted");
                WriteValue(output, specification.WhenStarted);
            }

            WriteArray(output, "outcomes", specification.Outcomes, (item, outcome) =>
            {
                item.WriteStartObject();
                CanonicalJson.WriteString(item, "node", outcome.Node);
                CanonicalJson.WriteString(item, "outlet", outcome.Outlet);
                item.WriteEndObject();
            });
            WriteArray(output, "then", specification.Then, (item, expectation) =>
            {
                item.WriteStartObject();
                item.WriteString("kind", expectation.Kind switch
                {
                    SemanticLifecycleExpectationKind.State => "state",
                    SemanticLifecycleExpectationKind.Sends => "sends",
                    SemanticLifecycleExpectationKind.Starts => "starts",
                    SemanticLifecycleExpectationKind.Completes => "completes",
                    SemanticLifecycleExpectationKind.Fails => "fails",
                    _ => "waits"
                });
                if (expectation.Target is not null) CanonicalJson.WriteString(item, "target", expectation.Target);
                if (expectation.Command.IsSet) item.WriteString("command", expectation.Command.ToString());
                item.WriteEndObject();
            });
            output.WriteEndObject();
        });
    }

    static void WriteWorkflowNode(Utf8JsonWriter writer, SemanticWorkflowNode node)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", node.Name);
        writer.WriteString("kind", WorkflowNodeKind(node.Kind));
        if (node.TriggerKind is { } trigger) writer.WriteString("trigger", WorkflowTriggerKind(trigger));
        if (node.Event.IsSet) writer.WriteString("event", node.Event.ToString());
        if (node.StateMachine is not null) CanonicalJson.WriteString(writer, "stateMachine", node.StateMachine);
        if (node.State is not null) CanonicalJson.WriteString(writer, "state", node.State);
        if (node.Every is { } every) writer.WriteNumber("every", every);
        if (node.At is { } at) writer.WriteNumber("at", at);
        if (node.OnDayOfWeek is { } dayOfWeek) writer.WriteNumber("onDayOfWeek", dayOfWeek);
        if (node.OnDayOfMonth is { } dayOfMonth) writer.WriteNumber("onDayOfMonth", dayOfMonth);
        if (node.Command.IsSet) writer.WriteString("command", node.Command.ToString());
        if (node.MaxAttempts is { } attempts) writer.WriteNumber("maxAttempts", attempts);
        if (node.AttemptTimeout is { } timeout) writer.WriteNumber("attemptTimeout", timeout);
        if (node.RetryDelay is { } retry) writer.WriteNumber("retryDelay", retry);
        if (node.Condition is not null)
        {
            writer.WritePropertyName("condition");
            WriteCondition(writer, node.Condition);
        }

        if (node.WhenTrue is not null) CanonicalJson.WriteString(writer, "whenTrue", node.WhenTrue);
        if (node.WhenFalse is not null) CanonicalJson.WriteString(writer, "whenFalse", node.WhenFalse);
        if (node.Duration is { } duration) writer.WriteNumber("duration", duration);
        if (node.ExpiresAfter is { } expires) writer.WriteNumber("expiresAfter", expires);
        if (node.ApproverKind is { } approverKind) writer.WriteString("approverKind", approverKind == SemanticApproverKind.Persona ? "persona" : "policy");
        if (node.Approver is not null) CanonicalJson.WriteString(writer, "approver", node.Approver);
        if (node.Items.IsSet) writer.WriteString("items", node.Items.ToString());
        if (node.MaxParallel is { } parallel) writer.WriteNumber("maxParallel", parallel);
        if (node.JoinMode is { } mode) writer.WriteString("joinMode", mode switch { SemanticJoinMode.All => "all", SemanticJoinMode.Any => "any", _ => "atLeast" });
        if (node.JoinCount is { } count) writer.WriteNumber("joinCount", count);
        writer.WriteEndObject();
    }

    static string WorkflowNodeKind(SemanticWorkflowNodeKind kind) => kind switch
    {
        SemanticWorkflowNodeKind.Trigger => "trigger",
        SemanticWorkflowNodeKind.Action => "action",
        SemanticWorkflowNodeKind.Decision => "decision",
        SemanticWorkflowNodeKind.Wait => "wait",
        SemanticWorkflowNodeKind.Approval => "approval",
        SemanticWorkflowNodeKind.ForEach => "forEach",
        SemanticWorkflowNodeKind.Join => "join",
        _ => throw Unknown(nameof(SemanticWorkflowNodeKind), kind)
    };

    static string WorkflowTriggerKind(SemanticWorkflowTriggerKind kind) => kind switch
    {
        SemanticWorkflowTriggerKind.Event => "event",
        SemanticWorkflowTriggerKind.StateEntered => "stateEntered",
        SemanticWorkflowTriggerKind.Schedule => "schedule",
        SemanticWorkflowTriggerKind.Manual => "manual",
        _ => throw Unknown(nameof(SemanticWorkflowTriggerKind), kind)
    };
}
