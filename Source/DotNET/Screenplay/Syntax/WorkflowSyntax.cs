// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the kind of a workflow node.
/// </summary>
public enum WorkflowNodeKind
{
    /// <summary>
    /// Starts a run.
    /// </summary>
    Trigger = 0,

    /// <summary>
    /// Sends a command.
    /// </summary>
    Action = 1,

    /// <summary>
    /// Chooses an outlet from a condition over the subject.
    /// </summary>
    Decide = 2,

    /// <summary>
    /// Waits for an event about the subject, or for time to pass.
    /// </summary>
    Wait = 3,

    /// <summary>
    /// Waits for a person to approve or reject.
    /// </summary>
    Approval = 4,

    /// <summary>
    /// Fans out over the items of a collection.
    /// </summary>
    ForEach = 5,

    /// <summary>
    /// Joins parallel branches back together.
    /// </summary>
    Join = 6
}

/// <summary>
/// Defines what starts a run.
/// </summary>
public enum WorkflowTriggerKind
{
    /// <summary>
    /// An event about the subject.
    /// </summary>
    Event = 0,

    /// <summary>
    /// A state machine entering a state.
    /// </summary>
    StateEntered = 1,

    /// <summary>
    /// A schedule: <c>every</c> an interval or <c>at</c> a time.
    /// </summary>
    Schedule = 2,

    /// <summary>
    /// A person or a command starts the run by hand.
    /// </summary>
    Manual = 3
}

/// <summary>
/// Defines how a join node decides it has enough branches.
/// </summary>
public enum WorkflowJoinMode
{
    /// <summary>
    /// Every incoming branch must arrive.
    /// </summary>
    All = 0,

    /// <summary>
    /// The first arriving branch is enough.
    /// </summary>
    Any = 1,

    /// <summary>
    /// A stated number of branches must arrive.
    /// </summary>
    AtLeast = 2
}

/// <summary>
/// Represents a <c>workflow</c> declaration - a durable graph of nodes that runs one at a time per subject.
/// </summary>
/// <param name="Name">The name of the workflow.</param>
/// <param name="Subject">The read model whose key identifies the subject of a run.</param>
/// <param name="Nodes">The <see cref="WorkflowNodeSyntax">nodes</see> of the graph, in authored order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record WorkflowSyntax(
    string Name,
    string Subject,
    IEnumerable<WorkflowNodeSyntax> Nodes,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional human readable description of the workflow.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the specifications that pin a run with Given/When/Then.
    /// </summary>
    public IEnumerable<LifecycleSpecificationSyntax> Specifications { get; init; } = [];
}

/// <summary>
/// Represents a length of time written as an amount and a unit.
/// </summary>
/// <param name="Amount">The amount, at least 1.</param>
/// <param name="Unit">The unit the amount counts in.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record WorkflowDurationSyntax(int Amount, IntervalUnit Unit, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an edge from an outlet of a node to another node: <c>&lt;outlet&gt; -&gt; &lt;Node&gt;</c>.
/// </summary>
/// <param name="Outlet">The outlet the edge leaves by.</param>
/// <param name="Target">The name of the node the edge leads to; empty when the outlet leads nowhere.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record WorkflowEdgeSyntax(string Outlet, string Target, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one named node of a workflow. Which members apply depends on <see cref="NodeKind"/>.
/// </summary>
/// <param name="NodeKind">The kind of node.</param>
/// <param name="Name">The name of the node.</param>
/// <param name="Edges">The <see cref="WorkflowEdgeSyntax">edges</see> leaving the node.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record WorkflowNodeSyntax(
    WorkflowNodeKind NodeKind,
    string Name,
    IEnumerable<WorkflowEdgeSyntax> Edges,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets what starts the run of a trigger node.
    /// </summary>
    public WorkflowTriggerKind? TriggerKind { get; init; }

    /// <summary>
    /// Gets the event a trigger starts on, or a wait waits for.
    /// </summary>
    public string? Event { get; init; }

    /// <summary>
    /// Gets the state machine whose entering of <see cref="State"/> starts a run.
    /// </summary>
    public string? StateMachine { get; init; }

    /// <summary>
    /// Gets the state of <see cref="StateMachine"/> a trigger waits to see entered.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Gets the <c>every</c> or <c>at</c> source of a scheduled trigger.
    /// </summary>
    public TriggerSourceSyntax? Schedule { get; init; }

    /// <summary>
    /// Gets the command an action sends.
    /// </summary>
    public string? Command { get; init; }

    /// <summary>
    /// Gets how many times an action may be attempted.
    /// </summary>
    public int? Attempts { get; init; }

    /// <summary>
    /// Gets how long one attempt of an action may take.
    /// </summary>
    public WorkflowDurationSyntax? Timeout { get; init; }

    /// <summary>
    /// Gets how long to wait before the next attempt of an action.
    /// </summary>
    public WorkflowDurationSyntax? RetryAfter { get; init; }

    /// <summary>
    /// Gets the condition over the subject a decide node evaluates.
    /// </summary>
    public ConditionSyntax? Condition { get; init; }

    /// <summary>
    /// Gets how long a wait for time lasts (<c>wait N for 2 days</c>), when it does not wait for an event.
    /// </summary>
    public WorkflowDurationSyntax? Duration { get; init; }

    /// <summary>
    /// Gets how long a wait or an approval lasts before its <c>expired</c> outlet is taken.
    /// </summary>
    public WorkflowDurationSyntax? ExpiresAfter { get; init; }

    /// <summary>
    /// Gets the persona or policy an approval is asked of.
    /// </summary>
    public string? By { get; init; }

    /// <summary>
    /// Gets the collection property of the subject a foreach node fans out over.
    /// </summary>
    public string? Items { get; init; }

    /// <summary>
    /// Gets the bound on how many items a foreach node processes at once.
    /// </summary>
    public int? Parallel { get; init; }

    /// <summary>
    /// Gets how a join node decides it has enough branches.
    /// </summary>
    public WorkflowJoinMode? JoinMode { get; init; }

    /// <summary>
    /// Gets how many branches an <see cref="WorkflowJoinMode.AtLeast"/> join needs.
    /// </summary>
    public int? JoinCount { get; init; }
}
