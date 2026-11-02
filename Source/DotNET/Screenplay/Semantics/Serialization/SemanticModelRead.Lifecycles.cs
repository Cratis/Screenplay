// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal static SemanticStateMachine StateMachine(ref Utf8JsonReader reader)
    {
        Object(ref reader, "state machine");
        var seen = NewSeen();
        string? name = null;
        SemanticId readModel = default;
        ImmutableArray<SemanticState> states = default;
        ImmutableArray<SemanticStateTransition> transitions = default;
        var specifications = ImmutableArray<SemanticLifecycleSpecification>.Empty;
        while (NextProperty(ref reader, seen, "state machine") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "readModel": readModel = SemanticId.Parse(String(ref reader, property)); break;
                case "states": states = Array(ref reader, State, property); break;
                case "transitions": transitions = Array(ref reader, StateTransition, property); break;
                case "specifications": specifications = Array(ref reader, LifecycleSpecification, property); break;
                default: throw Unknown(property, "state machine");
            }
        }

        Required(name is not null && readModel.IsSet && !states.IsDefault && !transitions.IsDefault, "state machine");
        return new(name!, readModel, states, transitions) { Specifications = specifications };
    }

    internal static SemanticWorkflow Workflow(ref Utf8JsonReader reader)
    {
        Object(ref reader, "workflow");
        var seen = NewSeen();
        string? name = null;
        SemanticId subject = default;
        ImmutableArray<SemanticWorkflowNode> nodes = default;
        ImmutableArray<SemanticWorkflowEdge> edges = default;
        var specifications = ImmutableArray<SemanticLifecycleSpecification>.Empty;
        while (NextProperty(ref reader, seen, "workflow") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "subject": subject = SemanticId.Parse(String(ref reader, property)); break;
                case "nodes": nodes = Array(ref reader, WorkflowNode, property); break;
                case "edges": edges = Array(ref reader, WorkflowEdge, property); break;
                case "specifications": specifications = Array(ref reader, LifecycleSpecification, property); break;
                default: throw Unknown(property, "workflow");
            }
        }

        Required(name is not null && subject.IsSet && !nodes.IsDefault && !edges.IsDefault, "workflow");
        return new(name!, subject, nodes, edges) { Specifications = specifications };
    }

    static SemanticState State(ref Utf8JsonReader reader)
    {
        Object(ref reader, "state");
        var seen = NewSeen();
        string? name = null;
        bool? initial = null;
        bool? final = null;
        ImmutableArray<SemanticStateEntry> entering = default;
        while (NextProperty(ref reader, seen, "state") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "initial": initial = Boolean(ref reader, property); break;
                case "final": final = Boolean(ref reader, property); break;
                case "entering": entering = Array(ref reader, StateEntry, property); break;
                default: throw Unknown(property, "state");
            }
        }

        Required(name is not null && initial is not null && final is not null && !entering.IsDefault, "state");
        return new(name!, initial!.Value, final!.Value, entering);
    }

    static SemanticStateEntry StateEntry(ref Utf8JsonReader reader)
    {
        Object(ref reader, "state entry");
        var seen = NewSeen();
        string? kind = null;
        SemanticId command = default;
        string? workflow = null;
        while (NextProperty(ref reader, seen, "state entry") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "command": command = SemanticId.Parse(String(ref reader, property)); break;
                case "workflow": workflow = String(ref reader, property); break;
                default: throw Unknown(property, "state entry");
            }
        }

        Required((kind == "send" && command.IsSet && workflow is null) || (kind == "start" && !command.IsSet && workflow is not null), "state entry");
        return new(kind == "send" ? SemanticStateEntryKind.SendCommand : SemanticStateEntryKind.StartWorkflow, command, workflow);
    }

    static SemanticStateTransition StateTransition(ref Utf8JsonReader reader)
    {
        Object(ref reader, "state transition");
        var seen = NewSeen();
        string? from = null;
        SemanticId @event = default;
        SemanticCondition? guard = null;
        string? to = null;
        while (NextProperty(ref reader, seen, "state transition") is { } property)
        {
            switch (property)
            {
                case "from": from = String(ref reader, property); break;
                case "event": @event = SemanticId.Parse(String(ref reader, property)); break;
                case "guard": RequiredToken(ref reader, JsonTokenType.StartObject, property); guard = Condition(ref reader); break;
                case "to": to = String(ref reader, property); break;
                default: throw Unknown(property, "state transition");
            }
        }

        Required(from is not null && @event.IsSet && to is not null, "state transition");
        return new(from!, @event, guard, to!);
    }

    static SemanticWorkflowEdge WorkflowEdge(ref Utf8JsonReader reader)
    {
        Object(ref reader, "workflow edge");
        var seen = NewSeen();
        string? from = null;
        string? outlet = null;
        string? to = null;
        while (NextProperty(ref reader, seen, "workflow edge") is { } property)
        {
            switch (property)
            {
                case "from": from = String(ref reader, property); break;
                case "outlet": outlet = String(ref reader, property); break;
                case "to": to = String(ref reader, property); break;
                default: throw Unknown(property, "workflow edge");
            }
        }

        Required(from is not null && outlet is not null && to is not null, "workflow edge");
        return new(from!, outlet!, to!);
    }

    static SemanticWorkflowNode WorkflowNode(ref Utf8JsonReader reader)
    {
        Object(ref reader, "workflow node");
        var seen = NewSeen();
        string? name = null;
        SemanticWorkflowNodeKind? kind = null;
        SemanticWorkflowTriggerKind? trigger = null;
        SemanticId @event = default;
        string? stateMachine = null;
        string? state = null;
        long? every = null;
        int? at = null;
        int? onDayOfWeek = null;
        int? onDayOfMonth = null;
        SemanticId command = default;
        int? maxAttempts = null;
        long? attemptTimeout = null;
        long? retryDelay = null;
        SemanticCondition? condition = null;
        string? whenTrue = null;
        string? whenFalse = null;
        long? duration = null;
        long? expiresAfter = null;
        SemanticApproverKind? approverKind = null;
        string? approver = null;
        SemanticId items = default;
        int? maxParallel = null;
        SemanticJoinMode? joinMode = null;
        int? joinCount = null;
        while (NextProperty(ref reader, seen, "workflow node") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "kind": kind = ParseWorkflowNodeKind(String(ref reader, property)); break;
                case "trigger": trigger = ParseWorkflowTriggerKind(String(ref reader, property)); break;
                case "event": @event = SemanticId.Parse(String(ref reader, property)); break;
                case "stateMachine": stateMachine = String(ref reader, property); break;
                case "state": state = String(ref reader, property); break;
                case "every": every = Int64(ref reader, property); break;
                case "at": at = Int32(ref reader, property); break;
                case "onDayOfWeek": onDayOfWeek = Int32(ref reader, property); break;
                case "onDayOfMonth": onDayOfMonth = Int32(ref reader, property); break;
                case "command": command = SemanticId.Parse(String(ref reader, property)); break;
                case "maxAttempts": maxAttempts = Int32(ref reader, property); break;
                case "attemptTimeout": attemptTimeout = Int64(ref reader, property); break;
                case "retryDelay": retryDelay = Int64(ref reader, property); break;
                case "condition": RequiredToken(ref reader, JsonTokenType.StartObject, property); condition = Condition(ref reader); break;
                case "whenTrue": whenTrue = String(ref reader, property); break;
                case "whenFalse": whenFalse = String(ref reader, property); break;
                case "duration": duration = Int64(ref reader, property); break;
                case "expiresAfter": expiresAfter = Int64(ref reader, property); break;
                case "approverKind": approverKind = String(ref reader, property) switch { "persona" => SemanticApproverKind.Persona, "policy" => SemanticApproverKind.Policy, var other => throw DiscriminatorError(other, "approver kind") }; break;
                case "approver": approver = String(ref reader, property); break;
                case "items": items = SemanticId.Parse(String(ref reader, property)); break;
                case "maxParallel": maxParallel = Int32(ref reader, property); break;
                case "joinMode": joinMode = String(ref reader, property) switch { "all" => SemanticJoinMode.All, "any" => SemanticJoinMode.Any, "atLeast" => SemanticJoinMode.AtLeast, var other => throw DiscriminatorError(other, "join mode") }; break;
                case "joinCount": joinCount = Int32(ref reader, property); break;
                default: throw Unknown(property, "workflow node");
            }
        }

        Required(name is not null && kind is not null, "workflow node");
        return new(name!, kind!.Value)
        {
            TriggerKind = trigger,
            Event = @event,
            StateMachine = stateMachine,
            State = state,
            Every = every,
            At = at,
            OnDayOfWeek = onDayOfWeek,
            OnDayOfMonth = onDayOfMonth,
            Command = command,
            MaxAttempts = maxAttempts,
            AttemptTimeout = attemptTimeout,
            RetryDelay = retryDelay,
            Condition = condition,
            WhenTrue = whenTrue,
            WhenFalse = whenFalse,
            Duration = duration,
            ExpiresAfter = expiresAfter,
            ApproverKind = approverKind,
            Approver = approver,
            Items = items,
            MaxParallel = maxParallel,
            JoinMode = joinMode,
            JoinCount = joinCount
        };
    }

    static SemanticWorkflowNodeKind ParseWorkflowNodeKind(string value) => value switch
    {
        "trigger" => SemanticWorkflowNodeKind.Trigger,
        "action" => SemanticWorkflowNodeKind.Action,
        "decision" => SemanticWorkflowNodeKind.Decision,
        "wait" => SemanticWorkflowNodeKind.Wait,
        "approval" => SemanticWorkflowNodeKind.Approval,
        "forEach" => SemanticWorkflowNodeKind.ForEach,
        "join" => SemanticWorkflowNodeKind.Join,
        _ => throw DiscriminatorError(value, "workflow node kind")
    };

    static SemanticWorkflowTriggerKind ParseWorkflowTriggerKind(string value) => value switch
    {
        "event" => SemanticWorkflowTriggerKind.Event,
        "stateEntered" => SemanticWorkflowTriggerKind.StateEntered,
        "schedule" => SemanticWorkflowTriggerKind.Schedule,
        "manual" => SemanticWorkflowTriggerKind.Manual,
        _ => throw DiscriminatorError(value, "workflow trigger kind")
    };

    static SemanticLifecycleSpecification LifecycleSpecification(ref Utf8JsonReader reader)
    {
        Object(ref reader, "lifecycle specification");
        var seen = NewSeen();
        string? name = null;
        ImmutableArray<SemanticSpecificationEvent> given = default;
        SemanticSpecificationEvent? whenAppended = null;
        SemanticValue? whenStarted = null;
        ImmutableArray<SemanticLifecycleOutcome> outcomes = default;
        ImmutableArray<SemanticLifecycleExpectation> then = default;
        while (NextProperty(ref reader, seen, "lifecycle specification") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "given": given = Array(ref reader, (ref Utf8JsonReader item) => SpecificationEvent(ref item, 36, false), property); break;
                case "whenAppended": RequiredToken(ref reader, JsonTokenType.StartObject, property); whenAppended = SpecificationEvent(ref reader, 36, false); break;
                case "whenStarted": whenStarted = Nested(ref reader, Value, property); break;
                case "outcomes": outcomes = Array(ref reader, LifecycleOutcome, property); break;
                case "then": then = Array(ref reader, LifecycleExpectation, property); break;
                default: throw Unknown(property, "lifecycle specification");
            }
        }

        Required(name is not null && !given.IsDefault && !outcomes.IsDefault && !then.IsDefault, "lifecycle specification");
        return new(name!, given) { WhenAppended = whenAppended, WhenStarted = whenStarted, Outcomes = outcomes, Then = then };
    }

    static SemanticLifecycleOutcome LifecycleOutcome(ref Utf8JsonReader reader)
    {
        Object(ref reader, "lifecycle outcome");
        var seen = NewSeen();
        string? node = null;
        string? outlet = null;
        while (NextProperty(ref reader, seen, "lifecycle outcome") is { } property)
        {
            switch (property)
            {
                case "node": node = String(ref reader, property); break;
                case "outlet": outlet = String(ref reader, property); break;
                default: throw Unknown(property, "lifecycle outcome");
            }
        }

        Required(node is not null && outlet is not null, "lifecycle outcome");
        return new(node!, outlet!);
    }

    static SemanticLifecycleExpectation LifecycleExpectation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "lifecycle expectation");
        var seen = NewSeen();
        SemanticLifecycleExpectationKind? kind = null;
        string? target = null;
        SemanticId command = default;
        while (NextProperty(ref reader, seen, "lifecycle expectation") is { } property)
        {
            switch (property)
            {
                case "kind":
                    kind = String(ref reader, property) switch
                    {
                        "state" => SemanticLifecycleExpectationKind.State,
                        "sends" => SemanticLifecycleExpectationKind.Sends,
                        "starts" => SemanticLifecycleExpectationKind.Starts,
                        "completes" => SemanticLifecycleExpectationKind.Completes,
                        "fails" => SemanticLifecycleExpectationKind.Fails,
                        "waits" => SemanticLifecycleExpectationKind.Waits,
                        var other => throw DiscriminatorError(other, "lifecycle expectation kind")
                    };
                    break;
                case "target": target = String(ref reader, property); break;
                case "command": command = SemanticId.Parse(String(ref reader, property)); break;
                default: throw Unknown(property, "lifecycle expectation");
            }
        }

        Required(kind is not null, "lifecycle expectation");
        return new(kind!.Value, target) { Command = command };
    }
}
