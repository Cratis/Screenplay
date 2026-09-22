// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Requests a bounded logical declaration rename with reference and identity continuity.
/// Unsupported or opaque occurrences are rejected, never guessed or silently normalized.
/// </summary>
public sealed record WorkspaceRenameRequest
{
    /// <summary>
    /// Gets the exact original workspace revision.
    /// </summary>
    public required WorkspaceRevision ExpectedRevision { get; init; }

    /// <summary>
    /// Gets the exact original identity catalog revision.
    /// </summary>
    public required Semantics.CatalogRevision ExpectedCatalogRevision { get; init; }

    /// <summary>
    /// Gets the original declaration occurrence; logical headers include every fragment.
    /// </summary>
    public required WorkspaceNodeHandle Target { get; init; }

    /// <summary>
    /// Gets the exact original declaration name.
    /// </summary>
    public required string ExpectedName { get; init; }

    /// <summary>
    /// Gets the new unqualified declaration name.
    /// </summary>
    public required string NewName { get; init; }

    /// <summary>
    /// Gets the explicit formatting policy. Trivia preservation never falls back to canonical printing.
    /// </summary>
    public WorkspaceAuthoringFormatting Formatting { get; init; } = WorkspaceAuthoringFormatting.PreserveTrivia;

    /// <summary>
    /// Gets the requested source or executable validation level.
    /// </summary>
    public WorkspaceAuthoringValidation Validation { get; init; } = WorkspaceAuthoringValidation.Authoring;
}
