// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines what entering a state does.
/// </summary>
public enum StateEntryKind
{
    /// <summary>
    /// Sends a command.
    /// </summary>
    Send = 0,

    /// <summary>
    /// Starts a workflow.
    /// </summary>
    Start = 1
}

/// <summary>
/// Represents a <c>statemachine</c> declaration - where a subject is in its lifecycle, derived from the read model it follows.
/// </summary>
/// <param name="Name">The name of the state machine.</param>
/// <param name="Follows">The read model the state machine follows; its key is the identity of one instance.</param>
/// <param name="States">The <see cref="StateSyntax">states</see> of the state machine.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A state machine never decides on commands. It follows facts that already happened: when an event arrives, the first
/// transition on it whose condition holds and whose target the current state allows is taken; anything else is ignored.
/// </remarks>
public record StateMachineSyntax(
    string Name,
    string Follows,
    IEnumerable<StateSyntax> States,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional human readable description of the state machine.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the specifications that pin the state machine with Given/When/Then.
    /// </summary>
    public IEnumerable<LifecycleSpecificationSyntax> Specifications { get; init; } = [];
}

/// <summary>
/// Represents one <c>state</c> of a state machine, with what entering it does and the transitions out of it.
/// </summary>
/// <param name="Name">The name of the state.</param>
/// <param name="IsInitial">Whether this is the state an instance starts in.</param>
/// <param name="IsFinal">Whether this state ends the lifecycle.</param>
/// <param name="Entering">The <see cref="StateEntrySyntax">entries</see> that run when the state is entered.</param>
/// <param name="Transitions">The <see cref="StateTransitionSyntax">transitions</see> out of the state.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record StateSyntax(
    string Name,
    bool IsInitial,
    bool IsFinal,
    IEnumerable<StateEntrySyntax> Entering,
    IEnumerable<StateTransitionSyntax> Transitions,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>entering send &lt;Command&gt;</c> or <c>entering start &lt;Workflow&gt;</c> line. It may run again on
/// redelivery, so what it names must be safe to repeat.
/// </summary>
/// <param name="EntryKind">Whether a command is sent or a workflow started.</param>
/// <param name="Target">The name of the command or workflow.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record StateEntrySyntax(StateEntryKind EntryKind, string Target, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents <c>on &lt;Event&gt; [when &lt;condition&gt;] -&gt; &lt;State&gt;</c>.
/// </summary>
/// <param name="Event">The name of the event that triggers the transition.</param>
/// <param name="When">The optional condition over the followed read model.</param>
/// <param name="Target">The name of the state the transition leads to.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record StateTransitionSyntax(
    string Event,
    ConditionSyntax? When,
    string Target,
    SourceLocation Location) : SyntaxNode(Location);
