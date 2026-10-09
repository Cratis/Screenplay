// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Requests an identity-preserving move of a logical slice or feature, including every source fragment.
/// </summary>
public sealed record WorkspaceMoveRequest
{
    /// <summary>
    /// Gets the original workspace revision.
    /// </summary>
    public required WorkspaceRevision ExpectedRevision { get; init; }

    /// <summary>
    /// Gets the original identity catalog revision.
    /// </summary>
    public required CatalogRevision ExpectedCatalogRevision { get; init; }

    /// <summary>
    /// Gets the logical subtree to move.
    /// </summary>
    public required SemanticAddress Target { get; init; }

    /// <summary>
    /// Gets the destination logical parent.
    /// </summary>
    public required SemanticAddress NewParent { get; init; }

    /// <summary>
    /// Gets an optional occurrence guard for the target address.
    /// </summary>
    public WorkspaceNodeHandle? TargetHandle { get; init; }

    /// <summary>
    /// Gets an optional occurrence guard for the destination address.
    /// </summary>
    public WorkspaceNodeHandle? NewParentHandle { get; init; }

    /// <summary>
    /// Gets the formatting policy; no fallback to canonical printing is permitted.
    /// </summary>
    public WorkspaceAuthoringFormatting Formatting { get; init; } = WorkspaceAuthoringFormatting.PreserveTrivia;

    /// <summary>
    /// Gets the requested validation level.
    /// </summary>
    public WorkspaceAuthoringValidation Validation { get; init; } = WorkspaceAuthoringValidation.Authoring;
}

/// <summary>
/// Reports the preserved identity and its previous and current addresses.
/// </summary>
/// <param name="Domain">The semantic or event identity domain.</param>
/// <param name="Id">The unchanged identity.</param>
/// <param name="PreviousAddress">The original address.</param>
/// <param name="CurrentAddress">The destination address.</param>
public sealed record WorkspaceMoveIdentityMigration(string Domain, string Id, SemanticAddress PreviousAddress, SemanticAddress CurrentAddress);

/// <summary>
/// Reports a repaired typed reference occurrence.
/// </summary>
/// <param name="Subject">The original occurrence handle.</param>
/// <param name="Member">The typed member path.</param>
/// <param name="Previous">The original spelling.</param>
/// <param name="Current">The repaired spelling.</param>
public sealed record WorkspaceMoveReferenceRepair(WorkspaceNodeHandle Subject, string Member, string Previous, string Current);

/// <summary>
/// Reports the complete logical move without any retired identity.
/// </summary>
/// <param name="IdentityMigrations">Every assigned descendant migration.</param>
/// <param name="ReferenceRepairs">The repaired typed references.</param>
/// <param name="FragmentsMoved">Every moved source fragment.</param>
public sealed record WorkspaceMoveReport(
    ImmutableArray<WorkspaceMoveIdentityMigration> IdentityMigrations,
    ImmutableArray<WorkspaceMoveReferenceRepair> ReferenceRepairs,
    ImmutableArray<WorkspaceNodeHandle> FragmentsMoved)
{
    /// <summary>
    /// Gets the empty retirement set; any identity loss refuses a move.
    /// </summary>
    public ImmutableArray<SemanticAddress> Retired => [];
}
