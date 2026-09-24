// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Defines the validation requested for an authoring transaction.
/// </summary>
public enum WorkspaceAuthoringValidation
{
    /// <summary>
    /// Require valid merged source, round-trip fidelity, and identity continuity, but not ESM support.
    /// </summary>
    Authoring,

    /// <summary>
    /// Additionally require successful executable semantic compilation.
    /// </summary>
    Executable
}

/// <summary>
/// Defines explicit permission to normalize touched source documents.
/// </summary>
public enum WorkspaceAuthoringFormatting
{
    /// <summary>
    /// Reject syntax changes that require printing.
    /// </summary>
    PreserveExactSource,

    /// <summary>
    /// Canonicalize touched documents, keeping attached comments while normalizing whitespace and preserving their UTF-8 BOM policy.
    /// </summary>
    CanonicalizeTouchedDocuments,

    /// <summary>
    /// Patch supported identifier members with verified structural round-trip fidelity, retaining every other byte.
    /// Unsupported spans are rejected; canonical printing requires separate explicit permission.
    /// </summary>
    PreserveTrivia
}

/// <summary>
/// Represents a snapshot occurrence, not a durable semantic identity. Paths use JSON Pointer member/index traversal;
/// the empty path identifies the document root. Every handle refers to the original revision, never an intermediate edit.
/// </summary>
/// <param name="Revision">The original workspace revision.</param>
/// <param name="Document">The original stable document identity.</param>
/// <param name="Path">The canonical JSON Pointer path in the typed syntax representation.</param>
public sealed record WorkspaceNodeHandle(WorkspaceRevision Revision, DocumentId Document, string Path);

/// <summary>
/// Defines a structurally typed syntax edit. Expectations always describe the base snapshot.
/// </summary>
public abstract record WorkspaceAstOperation;

/// <summary>
/// Adds a node to a named child slot. Collection indices are original insertion boundaries; null appends.
/// A singular slot must be empty. Multiple insertions at the same boundary are rejected as ambiguous.
/// </summary>
/// <param name="Parent">The original parent occurrence.</param>
/// <param name="ExpectedParent">The structural parent expected in the base snapshot.</param>
/// <param name="Member">The camel-case typed child member.</param>
/// <param name="Node">The node to insert.</param>
/// <param name="Index">The original collection insertion boundary, or null for append or a singular slot.</param>
public sealed record AddWorkspaceNode(WorkspaceNodeHandle Parent, SyntaxNode ExpectedParent, string Member, SyntaxNode Node, int? Index = null) : WorkspaceAstOperation;

/// <summary>
/// Replaces one occurrence, including a document root. Field edits use a replacement typed node.
/// References are not repaired automatically; supply coordinated typed replacements explicitly.
/// </summary>
/// <param name="Target">The original occurrence.</param>
/// <param name="Expected">The structural node expected in the base snapshot.</param>
/// <param name="Node">The replacement node.</param>
public sealed record ReplaceWorkspaceNode(WorkspaceNodeHandle Target, SyntaxNode Expected, SyntaxNode Node) : WorkspaceAstOperation;

/// <summary>
/// Removes one occurrence. Removing a document root requires a document removal operation instead.
/// Retirements of assigned identities must be declared explicitly in the request.
/// </summary>
/// <param name="Target">The original occurrence.</param>
/// <param name="Expected">The structural node expected in the base snapshot.</param>
public sealed record RemoveWorkspaceNode(WorkspaceNodeHandle Target, SyntaxNode Expected) : WorkspaceAstOperation;

/// <summary>
/// Moves one occurrence to an original parent, possibly in another document, without reference repair.
/// Address changes require explicit identity migrations for every assigned descendant.
/// </summary>
/// <param name="Target">The original occurrence.</param>
/// <param name="Expected">The structural node expected in the base snapshot.</param>
/// <param name="Parent">The original destination parent.</param>
/// <param name="ExpectedParent">The structural destination parent expected in the base snapshot.</param>
/// <param name="Member">The camel-case destination member.</param>
/// <param name="Index">The original collection insertion boundary, or null for append or a singular slot.</param>
public sealed record MoveWorkspaceNode(WorkspaceNodeHandle Target, SyntaxNode Expected, WorkspaceNodeHandle Parent, SyntaxNode ExpectedParent, string Member, int? Index = null) : WorkspaceAstOperation;

/// <summary>
/// Creates an entire document from typed syntax. Handles for the new document are available in the resulting workspace.
/// </summary>
/// <param name="StableKey">The new non-path stable key.</param>
/// <param name="Path">The new portable path.</param>
/// <param name="Syntax">The complete intended document syntax.</param>
/// <param name="Encoding">The explicit UTF-8 BOM policy.</param>
public sealed record CreateWorkspaceSyntaxDocument(string StableKey, PortablePlayPath Path, ApplicationSyntax Syntax, WorkspaceTextEncoding Encoding = WorkspaceTextEncoding.Utf8) : WorkspaceOperation;

/// <summary>
/// Replaces an existing document with typed syntax, including when its original text cannot be parsed.
/// The document identity, path, stable key, and encoding policy are preserved.
/// </summary>
/// <param name="Document">The existing document identity.</param>
/// <param name="Syntax">The complete intended document syntax.</param>
public sealed record ReplaceWorkspaceSyntaxDocument(DocumentId Document, ApplicationSyntax Syntax) : WorkspaceOperation;

/// <summary>
/// Represents an atomic full-language authoring request. All operations refer to the base snapshot.
/// Logical module/feature renames must update every fragment and migrate all affected assigned addresses.
/// </summary>
public sealed record WorkspaceAuthoringRequest
{
    /// <summary>
    /// Gets the exact base workspace revision.
    /// </summary>
    public required WorkspaceRevision ExpectedRevision { get; init; }

    /// <summary>
    /// Gets the exact base catalog revision.
    /// </summary>
    public required CatalogRevision ExpectedCatalogRevision { get; init; }

    /// <summary>
    /// Gets the explicitly requested acceptance level.
    /// </summary>
    public required WorkspaceAuthoringValidation Validation { get; init; }

    /// <summary>
    /// Gets explicit permission for canonical printing and comment loss in touched documents.
    /// </summary>
    public required WorkspaceAuthoringFormatting Formatting { get; init; }

    /// <summary>
    /// Gets the reference admission policy, independent from executable validation. Safe is the default.
    /// </summary>
    public WorkspaceAuthoringReferencePolicy ReferencePolicy { get; init; } = WorkspaceAuthoringReferencePolicy.Safe;

    /// <summary>
    /// Gets typed node operations.
    /// </summary>
    public ImmutableArray<WorkspaceAstOperation> Operations { get; init; } = [];

    /// <summary>
    /// Gets typed document creations or replacements and existing move, rename, or removal operations. Raw byte replacements and semantic text patches are not admitted.
    /// </summary>
    public ImmutableArray<WorkspaceOperation> Documents { get; init; } = [];

    /// <summary>
    /// Gets explicit semantic migrations, including assigned descendants of renamed or reparented declarations.
    /// </summary>
    public ImmutableArray<SemanticIdentityRename> SemanticRenames { get; init; } = [];

    /// <summary>
    /// Gets explicit event contract migrations.
    /// </summary>
    public ImmutableArray<EventContractIdentityRename> EventRenames { get; init; } = [];

    /// <summary>
    /// Gets semantic assignments explicitly retired by deliberate removals.
    /// </summary>
    public ImmutableArray<SemanticAddress> RetiredSemanticAddresses { get; init; } = [];

    /// <summary>
    /// Gets event contract assignments explicitly retired by deliberate removals.
    /// </summary>
    public ImmutableArray<SemanticAddress> RetiredEventAddresses { get; init; } = [];
}

/// <summary>
/// Represents separate source-authoring acceptance and executable readiness verdicts.
/// </summary>
public sealed record WorkspaceAuthoringResult
{
    /// <summary>
    /// Gets whether the complete transaction was admitted at the requested validation level.
    /// </summary>
    public bool Accepted => Workspace is not null && WritePlan is not null && Conflicts.IsEmpty;

    /// <summary>
    /// Gets the immutable accepted candidate; rejected requests never expose a writable partial candidate.
    /// </summary>
    public ScreenplayWorkspace? Workspace { get; init; }

    /// <summary>
    /// Gets the exact write plan only when accepted.
    /// </summary>
    public WorkspaceWritePlan? WritePlan { get; init; }

    /// <summary>
    /// Gets typed rejection reasons.
    /// </summary>
    public ImmutableArray<WorkspaceConflict> Conflicts { get; init; } = [];

    /// <summary>
    /// Gets source validation diagnostics and explicit normalization/comment-loss disclosures.
    /// </summary>
    public ImmutableArray<Diagnostic> AuthoringDiagnostics { get; init; } = [];

    /// <summary>
    /// Gets whether the final candidate successfully compiled to the executable semantic model.
    /// False also means executable compilation was not reached; inspect diagnostics and conflicts.
    /// </summary>
    public bool ExecutableReady { get; init; }

    /// <summary>
    /// Gets diagnostics from executable semantic compilation, independently of source acceptance.
    /// </summary>
    public ImmutableArray<Diagnostic> ExecutableDiagnostics { get; init; } = [];
}
