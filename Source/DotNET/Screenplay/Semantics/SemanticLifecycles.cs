// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines what entering a state does.
/// </summary>
public enum SemanticStateEntryKind
{
    /// <summary>
    /// Sends a command.
    /// </summary>
    SendCommand = 0,

    /// <summary>
    /// Starts a workflow.
    /// </summary>
    StartWorkflow = 1
}

/// <summary>
/// Defines the kind of a workflow node; each maps onto the node kind of the Cratis.Orleans workflow definition.
/// </summary>
public enum SemanticWorkflowNodeKind
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
    Decision = 2,

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
/// Defines what starts a workflow run.
/// </summary>
public enum SemanticWorkflowTriggerKind
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
    /// A schedule: an interval, or a time of day.
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
public enum SemanticJoinMode
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
    /// <see cref="SemanticWorkflowNode.JoinCount"/> branches must arrive.
    /// </summary>
    AtLeast = 2
}

/// <summary>
/// Defines the kind of party an approval is asked of.
/// </summary>
public enum SemanticApproverKind
{
    /// <summary>
    /// A persona.
    /// </summary>
    Persona = 0,

    /// <summary>
    /// A policy.
    /// </summary>
    Policy = 1
}

/// <summary>
/// Defines what a lifecycle specification expects.
/// </summary>
public enum SemanticLifecycleExpectationKind
{
    /// <summary>
    /// The state machine is in a state.
    /// </summary>
    State = 0,

    /// <summary>
    /// A command is sent; expectations of this kind match the sent commands in order.
    /// </summary>
    Sends = 1,

    /// <summary>
    /// A workflow is started; expectations of this kind match the started workflows in order.
    /// </summary>
    Starts = 2,

    /// <summary>
    /// The run ends without failing.
    /// </summary>
    Completes = 3,

    /// <summary>
    /// The run fails at a node.
    /// </summary>
    Fails = 4,

    /// <summary>
    /// The run is suspended at a wait or an approval.
    /// </summary>
    Waits = 5
}

/// <summary>
/// Represents a state machine: where one subject is in its lifecycle, derived from the read model it follows.
/// </summary>
/// <param name="Name">The unique state machine name.</param>
/// <param name="ReadModel">The followed read model; its key identifies one instance.</param>
/// <param name="States">The states in authored order; exactly one is initial.</param>
/// <param name="Transitions">The transitions in declaration order; the order is semantic.</param>
/// <remarks>
/// This is the model of the Cratis.Orleans <c>EventSourcedStateMachine</c>: a state allows the targets of the transitions
/// declared under it, and an event takes the first transition on it whose guard holds and whose target the current state allows.
/// </remarks>
public sealed record SemanticStateMachine(
    string Name,
    SemanticId ReadModel,
    ImmutableArray<SemanticState> States,
    ImmutableArray<SemanticStateTransition> Transitions)
{
    /// <summary>Gets the specifications that pin the state machine.</summary>
    public ImmutableArray<SemanticLifecycleSpecification> Specifications { get; init; } = [];
}

/// <summary>
/// Represents one state of a state machine.
/// </summary>
/// <param name="Name">The state name, unique within its state machine.</param>
/// <param name="IsInitial">Whether an instance starts in this state.</param>
/// <param name="IsFinal">Whether this state ends the lifecycle.</param>
/// <param name="Entering">What entering the state does, in authored order; it may run again on redelivery.</param>
public sealed record SemanticState(string Name, bool IsInitial, bool IsFinal, ImmutableArray<SemanticStateEntry> Entering);

/// <summary>
/// Represents one thing entering a state does.
/// </summary>
/// <param name="Kind">Whether a command is sent or a workflow started.</param>
/// <param name="Command">The command sent; unset when a workflow is started.</param>
/// <param name="Workflow">The name of the workflow started; null when a command is sent.</param>
public sealed record SemanticStateEntry(SemanticStateEntryKind Kind, SemanticId Command, string? Workflow);

/// <summary>
/// Represents a transition: <c>on &lt;Event&gt; [when &lt;guard&gt;] -&gt; &lt;State&gt;</c> declared under <see cref="From"/>.
/// </summary>
/// <param name="From">The state the transition is declared under; the state allows <paramref name="To"/>.</param>
/// <param name="Event">The event contract that triggers the transition.</param>
/// <param name="Guard">The condition over the followed read model, or null when unguarded.</param>
/// <param name="To">The target state.</param>
public sealed record SemanticStateTransition(string From, SemanticId Event, SemanticCondition? Guard, string To);

/// <summary>
/// Represents a workflow: a durable graph of nodes that runs one at a time per subject.
/// </summary>
/// <param name="Name">The unique workflow name.</param>
/// <param name="Subject">The read model whose key identifies the subject of a run.</param>
/// <param name="Nodes">The nodes in authored order.</param>
/// <param name="Edges">The edges, from an outlet of a node to another node, in authored order.</param>
/// <remarks>
/// The outlet names are those of Cratis.Orleans <c>WellKnownOutlets</c>: <c>next</c>, <c>failed</c>, <c>resumed</c>,
/// <c>expired</c>, <c>approved</c>, <c>rejected</c>, <c>each</c> and <c>completed</c>; a decision names its own. An unconnected
/// negative outlet (<c>failed</c>, <c>rejected</c>, <c>expired</c>) fails the run; an unconnected positive outlet ends that branch.
/// </remarks>
public sealed record SemanticWorkflow(
    string Name,
    SemanticId Subject,
    ImmutableArray<SemanticWorkflowNode> Nodes,
    ImmutableArray<SemanticWorkflowEdge> Edges)
{
    /// <summary>Gets the specifications that pin a run.</summary>
    public ImmutableArray<SemanticLifecycleSpecification> Specifications { get; init; } = [];
}

/// <summary>
/// Represents one node of a workflow. Which members apply depends on <see cref="Kind"/>; the others are unset.
/// </summary>
/// <param name="Name">The node name, unique within its workflow.</param>
/// <param name="Kind">The kind of node.</param>
public sealed record SemanticWorkflowNode(string Name, SemanticWorkflowNodeKind Kind)
{
    /// <summary>Gets what starts the run of a trigger node.</summary>
    public SemanticWorkflowTriggerKind? TriggerKind { get; init; }

    /// <summary>Gets the event contract a trigger starts on, or a wait waits for.</summary>
    public SemanticId Event { get; init; }

    /// <summary>Gets the state machine whose entering of <see cref="State"/> starts a run.</summary>
    public string? StateMachine { get; init; }

    /// <summary>Gets the state a trigger waits to see entered.</summary>
    public string? State { get; init; }

    /// <summary>Gets the interval in seconds of a scheduled trigger.</summary>
    public long? Every { get; init; }

    /// <summary>Gets the second of the UTC day a scheduled trigger fires at.</summary>
    public int? At { get; init; }

    /// <summary>Gets the day of the week a scheduled trigger fires on, Sunday being 0.</summary>
    public int? OnDayOfWeek { get; init; }

    /// <summary>Gets the day of the month a scheduled trigger fires on.</summary>
    public int? OnDayOfMonth { get; init; }

    /// <summary>Gets the command an action sends.</summary>
    public SemanticId Command { get; init; }

    /// <summary>Gets how many times an action may be attempted; null means once.</summary>
    public int? MaxAttempts { get; init; }

    /// <summary>Gets how long one attempt of an action may take, in seconds.</summary>
    public long? AttemptTimeout { get; init; }

    /// <summary>Gets how long to wait before the next attempt, in seconds.</summary>
    public long? RetryDelay { get; init; }

    /// <summary>Gets the condition a decision evaluates over the subject.</summary>
    public SemanticCondition? Condition { get; init; }

    /// <summary>Gets the outlet a decision takes when its condition holds.</summary>
    public string? WhenTrue { get; init; }

    /// <summary>Gets the outlet a decision takes when its condition does not hold.</summary>
    public string? WhenFalse { get; init; }

    /// <summary>Gets how long a wait for time lasts, in seconds.</summary>
    public long? Duration { get; init; }

    /// <summary>Gets how long a wait or approval lasts before its expired outlet is taken, in seconds.</summary>
    public long? ExpiresAfter { get; init; }

    /// <summary>Gets the kind of party an approval is asked of.</summary>
    public SemanticApproverKind? ApproverKind { get; init; }

    /// <summary>Gets the name of the persona or policy an approval is asked of.</summary>
    public string? Approver { get; init; }

    /// <summary>Gets the collection property of the subject a foreach fans out over.</summary>
    public SemanticId Items { get; init; }

    /// <summary>Gets the bound on how many items a foreach processes at once.</summary>
    public int? MaxParallel { get; init; }

    /// <summary>Gets how a join decides it has enough branches.</summary>
    public SemanticJoinMode? JoinMode { get; init; }

    /// <summary>Gets how many branches an at-least join needs.</summary>
    public int? JoinCount { get; init; }
}

/// <summary>
/// Represents an edge from an outlet of a node to another node.
/// </summary>
/// <param name="From">The node the edge leaves.</param>
/// <param name="Outlet">The outlet it leaves by.</param>
/// <param name="To">The node it leads to.</param>
public sealed record SemanticWorkflowEdge(string From, string Outlet, string To);

/// <summary>
/// Represents the outlet a node takes when the run reaches it.
/// </summary>
/// <param name="Node">The node name.</param>
/// <param name="Outlet">The outlet it takes.</param>
public sealed record SemanticLifecycleOutcome(string Node, string Outlet);

/// <summary>
/// Represents one expectation of a lifecycle specification.
/// </summary>
/// <param name="Kind">What is expected.</param>
/// <param name="Target">The state, node or workflow name; the command identity text is carried by <see cref="Command"/>.</param>
public sealed record SemanticLifecycleExpectation(SemanticLifecycleExpectationKind Kind, string? Target)
{
    /// <summary>Gets the command a <see cref="SemanticLifecycleExpectationKind.Sends"/> expectation names.</summary>
    public SemanticId Command { get; init; }
}

/// <summary>
/// Represents a specification of a state machine or a workflow: facts that happened, one event or a start, the stated
/// outcomes of nodes and what follows.
/// </summary>
/// <param name="Name">The specification name.</param>
/// <param name="Given">The events that already happened, in order.</param>
public sealed record SemanticLifecycleSpecification(string Name, ImmutableArray<SemanticSpecificationEvent> Given)
{
    /// <summary>Gets the event the specification appends.</summary>
    public SemanticSpecificationEvent? WhenAppended { get; init; }

    /// <summary>Gets the subject key a run is started for by hand.</summary>
    public SemanticValue? WhenStarted { get; init; }

    /// <summary>Gets the stated outcomes of nodes the run reaches.</summary>
    public ImmutableArray<SemanticLifecycleOutcome> Outcomes { get; init; } = [];

    /// <summary>Gets what the specification expects, in order.</summary>
    public ImmutableArray<SemanticLifecycleExpectation> Then { get; init; } = [];
}
