// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Defines one immutable whole-document workspace operation.
/// </summary>
public abstract record WorkspaceOperation;

/// <summary>
/// Adds one document with a new stable identity.
/// </summary>
public sealed record AddWorkspaceDocument : WorkspaceOperation
{
    /// <summary>
    /// Gets the new stable non-path document key.
    /// </summary>
    public required string StableKey { get; init; }

    /// <summary>
    /// Gets the new portable document path.
    /// </summary>
    public required PortablePlayPath Path { get; init; }

    /// <summary>
    /// Gets the exact UTF-8 document bytes.
    /// </summary>
    public required ImmutableArray<byte> Bytes { get; init; }
}

/// <summary>
/// Replaces the exact bytes of one existing document.
/// </summary>
public sealed record ReplaceWorkspaceDocument : WorkspaceOperation
{
    /// <summary>
    /// Gets the document identity.
    /// </summary>
    public required DocumentId Document { get; init; }

    /// <summary>
    /// Gets the replacement UTF-8 bytes.
    /// </summary>
    public required ImmutableArray<byte> Bytes { get; init; }
}

/// <summary>
/// Moves one existing document without changing its identity or stable key.
/// </summary>
public sealed record MoveWorkspaceDocument : WorkspaceOperation
{
    /// <summary>
    /// Gets the document identity.
    /// </summary>
    public required DocumentId Document { get; init; }

    /// <summary>
    /// Gets the new portable path.
    /// </summary>
    public required PortablePlayPath Path { get; init; }
}

/// <summary>
/// Renames one document's stable key while preserving its explicit identity and path.
/// </summary>
public sealed record RenameWorkspaceDocument : WorkspaceOperation
{
    /// <summary>
    /// Gets the document identity.
    /// </summary>
    public required DocumentId Document { get; init; }

    /// <summary>
    /// Gets the new stable non-path key.
    /// </summary>
    public required string StableKey { get; init; }
}

/// <summary>
/// Removes one existing document.
/// </summary>
public sealed record RemoveWorkspaceDocument : WorkspaceOperation
{
    /// <summary>
    /// Gets the document identity.
    /// </summary>
    public required DocumentId Document { get; init; }
}

/// <summary>
/// Represents one revision-bound workspace transaction request.
/// </summary>
public sealed record WorkspaceTransactionRequest
{
    /// <summary>
    /// Gets the exact workspace revision this request was authored against.
    /// </summary>
    public required WorkspaceRevision ExpectedRevision { get; init; }

    /// <summary>
    /// Gets the exact identity-catalog revision this request was authored against.
    /// </summary>
    public required CatalogRevision ExpectedCatalogRevision { get; init; }

    /// <summary>
    /// Gets whole-document operations. One transaction may target each document at most once.
    /// </summary>
    public ImmutableArray<WorkspaceOperation> Operations { get; init; } = [];

    /// <summary>
    /// Gets explicit semantic-address renames required to preserve identity continuity.
    /// </summary>
    public ImmutableArray<SemanticIdentityRename> SemanticRenames { get; init; } = [];

    /// <summary>
    /// Gets explicit event-contract address renames required to preserve contract continuity.
    /// </summary>
    public ImmutableArray<EventContractIdentityRename> EventRenames { get; init; } = [];

    /// <summary>
    /// Gets semantic addresses explicitly retired by this transaction.
    /// </summary>
    public ImmutableArray<SemanticAddress> RetiredSemanticAddresses { get; init; } = [];

    /// <summary>
    /// Gets event-contract addresses explicitly retired by this transaction.
    /// </summary>
    public ImmutableArray<SemanticAddress> RetiredEventAddresses { get; init; } = [];
}

/// <summary>
/// Represents one typed workspace transaction conflict.
/// </summary>
public sealed record WorkspaceConflict
{
    /// <summary>
    /// Gets the conflict kind.
    /// </summary>
    public required WorkspaceConflictKind Kind { get; init; }

    /// <summary>
    /// Gets the human-readable conflict description.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the affected document identity, when one is known.
    /// </summary>
    public DocumentId? Document { get; init; }

    /// <summary>
    /// Gets the affected portable path, when one is known.
    /// </summary>
    public PortablePlayPath? Path { get; init; }
}

/// <summary>
/// Represents one exact document change in a pure workspace write plan.
/// </summary>
public sealed record WorkspaceWriteEntry
{
    /// <summary>
    /// Gets the stable document identity.
    /// </summary>
    public required DocumentId Document { get; init; }

    /// <summary>
    /// Gets the change kind.
    /// </summary>
    public required WorkspaceWriteKind Kind { get; init; }

    /// <summary>
    /// Gets the exact document before the change, when it existed.
    /// </summary>
    public WorkspaceDocument? Before { get; init; }

    /// <summary>
    /// Gets the exact document after the change, when it exists.
    /// </summary>
    public WorkspaceDocument? After { get; init; }
}

/// <summary>
/// Represents a pure destination-independent workspace write plan.
/// </summary>
public sealed record WorkspaceWritePlan
{
    /// <summary>
    /// Gets the workspace revision before the transaction.
    /// </summary>
    public required WorkspaceRevision BeforeRevision { get; init; }

    /// <summary>
    /// Gets the workspace revision after the transaction.
    /// </summary>
    public required WorkspaceRevision AfterRevision { get; init; }

    /// <summary>
    /// Gets the identity-catalog revision before the transaction.
    /// </summary>
    public required CatalogRevision BeforeCatalogRevision { get; init; }

    /// <summary>
    /// Gets the identity-catalog revision after the transaction.
    /// </summary>
    public required CatalogRevision AfterCatalogRevision { get; init; }

    /// <summary>
    /// Gets exact document changes in stable document-identity order.
    /// </summary>
    public ImmutableArray<WorkspaceWriteEntry> Entries { get; init; } = [];
}

/// <summary>
/// Represents the outcome of proposing one atomic workspace transaction.
/// </summary>
public sealed record WorkspaceTransactionResult
{
    /// <summary>
    /// Gets a value indicating whether the complete transaction succeeded.
    /// </summary>
    public bool Success => Workspace is not null && WritePlan is not null && Conflicts.IsEmpty;

    /// <summary>
    /// Gets the immutable candidate workspace when the transaction succeeded.
    /// </summary>
    public ScreenplayWorkspace? Workspace { get; init; }

    /// <summary>
    /// Gets the exact destination-independent write plan when the transaction succeeded.
    /// </summary>
    public WorkspaceWritePlan? WritePlan { get; init; }

    /// <summary>
    /// Gets typed conflicts when the transaction failed.
    /// </summary>
    public ImmutableArray<WorkspaceConflict> Conflicts { get; init; } = [];

    /// <summary>
    /// Gets parser, merge, binder, and semantic diagnostics associated with a failed candidate.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; init; } = [];
}
