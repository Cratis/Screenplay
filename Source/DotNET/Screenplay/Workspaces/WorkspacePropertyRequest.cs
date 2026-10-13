// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Selects one original property occurrence explicitly.
/// </summary>
/// <param name="Target">The revision-bound property handle.</param>
/// <param name="ExpectedName">The exact original name.</param>
public sealed record WorkspacePropertySelection(WorkspaceNodeHandle Target, string ExpectedName);

/// <summary>
/// Requests a safe add, rename or removal of explicitly selected properties on one resolved mapping chain.
/// </summary>
public sealed record WorkspacePropertyRequest
{
    /// <summary>
    /// Gets the original workspace revision.
    /// </summary>
    public required WorkspaceRevision ExpectedRevision { get; init; }

    /// <summary>
    /// Gets the original catalog revision.
    /// </summary>
    public required CatalogRevision ExpectedCatalogRevision { get; init; }

    /// <summary>
    /// Gets the action, add, rename or remove.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the original property occurrence.
    /// </summary>
    public WorkspaceNodeHandle? Target { get; init; }

    /// <summary>
    /// Gets the exact original name.
    /// </summary>
    public string? ExpectedName { get; init; }

    /// <summary>
    /// Gets the declaration receiving an added property.
    /// </summary>
    public WorkspaceNodeHandle? Owner { get; init; }

    /// <summary>
    /// Gets the exact original owner name for add.
    /// </summary>
    public string? ExpectedOwnerName { get; init; }

    /// <summary>
    /// Gets the added property's unqualified name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the added property's authored type text.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets whether the added property is optional.
    /// </summary>
    public bool Optional { get; init; }

    /// <summary>
    /// Gets explicitly selected property and mapping additions on the owner's chain.
    /// </summary>
    public ImmutableArray<WorkspacePropertyHop> Hops { get; init; } = [];

    /// <summary>
    /// Gets literal source text to add to every required fixture and assertion.
    /// </summary>
    public string? FixtureValue { get; init; }

    /// <summary>
    /// Gets the new unqualified name for rename.
    /// </summary>
    public string? NewName { get; init; }

    /// <summary>
    /// Gets additional explicitly selected properties for rename.
    /// </summary>
    public ImmutableArray<WorkspacePropertySelection> AlsoRename { get; init; } = [];

    /// <summary>
    /// Gets additional explicitly selected properties for removal.
    /// </summary>
    public ImmutableArray<WorkspacePropertySelection> AlsoRemove { get; init; } = [];

    /// <summary>
    /// Gets whether each selected current event contract advances exactly once.
    /// </summary>
    public bool NewGeneration { get; init; }

    /// <summary>
    /// Gets the caller's assertion that selected current event generations were never stored.
    /// </summary>
    public bool EventNeverPersisted { get; init; }

    /// <summary>Gets the independently read release baseline; null preserves legacy behavior.</summary>
    public ReleaseBaselineRead? ReleaseBaseline { get; init; }

    /// <summary>
    /// Gets explicit formatting consent; unsupported trivia edits never fall back to printing.
    /// </summary>
    public WorkspaceAuthoringFormatting Formatting { get; init; } = WorkspaceAuthoringFormatting.PreserveTrivia;

    /// <summary>
    /// Gets the requested acceptance level, separate from executable readiness.
    /// </summary>
    public WorkspaceAuthoringValidation Validation { get; init; } = WorkspaceAuthoringValidation.Authoring;
}
