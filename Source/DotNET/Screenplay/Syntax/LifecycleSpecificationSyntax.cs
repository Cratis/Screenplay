// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines what a lifecycle specification expects.
/// </summary>
public enum LifecycleExpectationKind
{
    /// <summary>
    /// The state machine is in a state: <c>then is &lt;State&gt;</c>.
    /// </summary>
    State = 0,

    /// <summary>
    /// A command is invoked, in order: <c>then invokes &lt;Command&gt;</c>.
    /// </summary>
    Sends = 1,

    /// <summary>
    /// A workflow is started, in order: <c>then starts &lt;Workflow&gt;</c>.
    /// </summary>
    Starts = 2,

    /// <summary>
    /// The run ends without failing: <c>then completes</c>.
    /// </summary>
    Completes = 3,

    /// <summary>
    /// The run fails at a node: <c>then fails at &lt;Node&gt;</c>.
    /// </summary>
    Fails = 4,

    /// <summary>
    /// The run is suspended at a wait or an approval: <c>then waits at &lt;Node&gt;</c>.
    /// </summary>
    Waits = 5
}

/// <summary>
/// Represents a specification written inside a <c>statemachine</c> or a <c>workflow</c>: facts that happened, one event
/// or a start, the stated outcomes of waits, approvals and actions, and what follows.
/// </summary>
/// <param name="Name">The name of the specification.</param>
/// <param name="Given">The events that already happened, in order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record LifecycleSpecificationSyntax(
    string Name,
    IEnumerable<SpecificationEventSyntax> Given,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional human readable description of the specification.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the event the specification appends: <c>when append &lt;Event&gt;</c>.
    /// </summary>
    public SpecificationEventSyntax? WhenAppended { get; init; }

    /// <summary>
    /// Gets the subject key a workflow run is started for by hand: <c>when start &lt;key&gt;</c>.
    /// </summary>
    public ExpressionSyntax? WhenStarted { get; init; }

    /// <summary>
    /// Gets the stated outcomes of nodes the run reaches.
    /// </summary>
    public IEnumerable<LifecycleOutcomeSyntax> Outcomes { get; init; } = [];

    /// <summary>
    /// Gets what the specification expects, in order.
    /// </summary>
    public IEnumerable<LifecycleExpectationSyntax> Then { get; init; } = [];
}

/// <summary>
/// Represents <c>outcome &lt;Node&gt; &lt;outlet&gt;</c> - the outlet a node takes when the run reaches it.
/// </summary>
/// <param name="Node">The name of the node.</param>
/// <param name="Outlet">The outlet the node takes.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record LifecycleOutcomeSyntax(string Node, string Outlet, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one <c>then</c> line of a lifecycle specification.
/// </summary>
/// <param name="Expectation">What is expected.</param>
/// <param name="Target">The state, command, workflow or node the expectation names; null for <see cref="LifecycleExpectationKind.Completes"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record LifecycleExpectationSyntax(LifecycleExpectationKind Expectation, string? Target, SourceLocation Location) : SyntaxNode(Location);
