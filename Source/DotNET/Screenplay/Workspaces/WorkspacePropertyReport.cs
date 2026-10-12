// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Identifies a selected property and its physical generation.
/// </summary>
/// <param name="Address">The canonical property address.</param>
/// <param name="Kind">The property's owner kind.</param>
/// <param name="Generation">The event generation, or null.</param>
public sealed record WorkspacePropertyTarget(SemanticAddress Address, string Kind, uint? Generation);

/// <summary>
/// Describes one proven affected occurrence.
/// </summary>
/// <param name="Address">The canonical property address.</param>
/// <param name="Kind">The property's owner kind.</param>
/// <param name="Role">The indexed role.</param>
/// <param name="Location">The original source location.</param>
public sealed record WorkspacePropertyCandidate(SemanticAddress Address, string Kind, string Role, SourceLocation Location);

/// <summary>
/// Discloses one planned change, including weakened assertions.
/// </summary>
/// <param name="Address">The canonical property address.</param>
/// <param name="Kind">The property's owner kind.</param>
/// <param name="Role">The indexed role.</param>
/// <param name="Change">The change classification.</param>
/// <param name="Location">The original source location.</param>
/// <param name="WeakensAssertion">Whether an authored expected value is removed.</param>
public sealed record WorkspacePropertyChange(SemanticAddress Address, string Kind, string Role, string Change, SourceLocation Location, bool WeakensAssertion = false);

/// <summary>
/// Reports an untouched off-chain property or a possible follow-up.
/// </summary>
/// <param name="Address">The canonical property address.</param>
/// <param name="Kind">The property's owner kind.</param>
/// <param name="Reason">Why it was not selected.</param>
/// <param name="Role">The indexed role, when applicable.</param>
public sealed record WorkspacePropertyUnchanged(SemanticAddress Address, string Kind, string Reason, string? Role = null);

/// <summary>
/// Names a refusal and all its relevant original occurrences.
/// </summary>
/// <param name="Reason">The typed refusal reason.</param>
/// <param name="Subject">The selected property, when resolved.</param>
/// <param name="Candidates">The evidence requiring a decision.</param>
public sealed record WorkspacePropertyDecision(string Reason, WorkspacePropertyTarget? Subject, ImmutableArray<WorkspacePropertyCandidate> Candidates);

/// <summary>
/// Reports one explicitly selected event owner's evolution.
/// </summary>
/// <param name="Event">The canonical event contract address.</param>
/// <param name="From">The original generation.</param>
/// <param name="To">The planned generation.</param>
/// <param name="Mode">The explicit event evolution choice.</param>
public sealed record WorkspacePropertyGeneration(SemanticAddress Event, uint From, uint To, string Mode);

/// <summary>
/// Reports the selected executable-model version change.
/// </summary>
/// <param name="From">The previous version.</param>
/// <param name="To">The candidate version.</param>
public sealed record WorkspacePropertyEsmVersionChange(string From, string To);

/// <summary>
/// Carries the same selected-set plan through proposal creation and retained review.
/// </summary>
public sealed record WorkspacePropertyReport
{
    /// <summary>
    /// Gets the requested action.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the original target, when resolved.
    /// </summary>
    public WorkspacePropertyTarget? Target { get; init; }

    /// <summary>
    /// Gets the planned occurrence changes.
    /// </summary>
    public ImmutableArray<WorkspacePropertyChange> Changed { get; init; } = [];

    /// <summary>
    /// Gets same-named off-chain properties, never edited.
    /// </summary>
    public ImmutableArray<WorkspacePropertyUnchanged> NotChanged { get; init; } = [];

    /// <summary>
    /// Gets unresolved decisions; refusals have no writable candidate.
    /// </summary>
    public ImmutableArray<WorkspacePropertyDecision> NeedsDecision { get; init; } = [];

    /// <summary>
    /// Gets unselected chain members as follow-up candidates, not permission to change them.
    /// </summary>
    public ImmutableArray<WorkspacePropertyUnchanged> ChainCandidates { get; init; } = [];

    /// <summary>
    /// Gets every accepted planned event edit, deduplicated by owner and canonically ordered. Empty is not a success verdict.
    /// </summary>
    public ImmutableArray<WorkspacePropertyGeneration> Generations { get; init; } = [];

    /// <summary>
    /// Gets whether the candidate has no event requiring an unavailable transformation.
    /// </summary>
    public bool DeployableWithoutTransformation { get; init; }

    /// <summary>
    /// Gets the version change, or null when unchanged or unknown.
    /// </summary>
    public WorkspacePropertyEsmVersionChange? EsmVersionChange { get; init; }

    /// <summary>
    /// Gets whether the candidate bound and its selected version is known.
    /// </summary>
    public bool EsmVersionKnown { get; init; }
}
