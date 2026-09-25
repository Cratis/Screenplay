// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// A compiler-authored, revision-bound repair. Operations address original syntax occurrences and are
/// previewed through <see cref="WorkspaceDiagnosticRepairs.ProposeRepair"/>, never applied by discovery.
/// The PLAY0397 operation replaces its subject with itself: canonical printing migrates the entire touched
/// document, potentially normalizing other legacy forms. A repair is refused if any comment would be lost.
/// </summary>
public sealed record WorkspaceDiagnosticRepair(
    string DiagnosticCode,
    WorkspaceNodeHandle Subject,
    ImmutableArray<WorkspaceAstOperation> Operations)
{
    /// <summary>
    /// Gets the formatting required to make this repair effective. PreserveTrivia cannot migrate this warning.
    /// </summary>
    public WorkspaceAuthoringFormatting RequiredFormatting { get; init; } = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments;
}

/// <summary>
/// Discovers deterministic repairs by diagnostic code and original syntax occurrence, not message text.
/// </summary>
public static class WorkspaceDiagnosticRepairs
{
    /// <summary>
    /// Previews a revision-bound repair without writing files. The identity replacement causes canonical printing
    /// of the entire touched document; other legacy forms and whitespace may change. Refuses any dropped comment,
    /// even outside the repair subject, and refuses formatting other than the repair's required formatting.
    /// </summary>
    /// <param name="workspace">The original workspace.</param>
    /// <param name="repair">A repair discovered for this workspace revision.</param>
    /// <param name="request">The authoring request with expected revisions and explicit formatting consent.</param>
    /// <returns>An accepted candidate and write plan, or typed conflicts without a partial candidate.</returns>
    public static WorkspaceAuthoringResult ProposeRepair(ScreenplayWorkspace workspace, WorkspaceDiagnosticRepair repair, WorkspaceAuthoringRequest request)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(repair);
        ArgumentNullException.ThrowIfNull(request);

        // Preserve the transaction's typed stale-revision result before checking the subject.
        if (request.ExpectedRevision != workspace.Revision || request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return workspace.ProposeAuthoring(request with { Operations = repair.Operations });
        }

        if (request.Formatting != repair.RequiredFormatting || request.Formatting != WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            return Refuse(WorkspaceConflictKind.FormattingConsentRequired, "Canonical formatting consent is required for this diagnostic repair.");
        }

        var index = WorkspaceSyntaxIndex.Create(workspace);
        var matches = index.Diagnostics.Where(diagnostic => diagnostic.Code == repair.DiagnosticCode)
            .SelectMany(diagnostic => Find(index, workspace.Revision, diagnostic))
            .Where(candidate => candidate.Subject == repair.Subject).ToArray();
        if (matches.Length != 1 || repair.Operations.IsDefaultOrEmpty ||
            repair.Operations.Any(operation => operation is not ReplaceWorkspaceNode replace || replace.Target != repair.Subject || !ReferenceEquals(replace.Expected, replace.Node)))
        {
            return Refuse(WorkspaceConflictKind.InvalidOperation, "The diagnostic repair does not match the original workspace subject.");
        }

        var result = workspace.ProposeAuthoring(request with { Operations = matches[0].Operations });
        if (result.Accepted && !WorkspaceDroppedComments.In(result.WritePlan!).IsEmpty)
        {
            return Refuse(WorkspaceConflictKind.RepairWouldDropComments, "The diagnostic repair would drop comments from the touched document.");
        }

        return result;
    }

    /// <summary>
    /// Finds repairs for a diagnostic from this exact workspace revision. Unknown or ambiguous subjects
    /// and diagnostics not produced by this workspace have no repair.
    /// </summary>
    /// <param name="workspace">The original workspace.</param>
    /// <param name="revision">The revision that supplied the diagnostic.</param>
    /// <param name="diagnostic">The original diagnostic.</param>
    /// <returns>Zero or more typed repair proposals.</returns>
    public static ImmutableArray<WorkspaceDiagnosticRepair> Find(ScreenplayWorkspace workspace, WorkspaceRevision revision, Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return revision == workspace.Revision ? Find(WorkspaceSyntaxIndex.Create(workspace), revision, diagnostic) : [];
    }

    /// <summary>
    /// Finds repairs using a previously built occurrence index for the expected revision.
    /// The diagnostic must be present in that index.
    /// </summary>
    /// <param name="index">The original workspace occurrence index.</param>
    /// <param name="revision">The expected workspace revision.</param>
    /// <param name="diagnostic">A diagnostic reported by the index.</param>
    /// <returns>Zero or more typed repair proposals.</returns>
    public static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(index);
        if (diagnostic is null || diagnostic.Code != DiagnosticCodes.LegacyInlineCodeFence)
        {
            return [];
        }

        // PLAY0397 also covers bare description fences and legacy handler language lines. Only the
        // 'validate csharp' form has a unique CodeValidateSyntax subject at the warning's position.
        if (!index.Diagnostics.Any(item => item.Code == diagnostic.Code && item.Location == diagnostic.Location))
        {
            return [];
        }

        var matching = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Node is CodeValidateSyntax && entry.Location == diagnostic.Location).ToArray();
        if (matching.Length != 1)
        {
            return [];
        }

        var subject = matching[0];
        return [new WorkspaceDiagnosticRepair(
            diagnostic.Code,
            subject.Handle,
            [new ReplaceWorkspaceNode(subject.Handle, subject.Node, subject.Node)])];
    }

    static WorkspaceAuthoringResult Refuse(WorkspaceConflictKind kind, string message) => new()
    {
        Conflicts = [new WorkspaceConflict { Kind = kind, Message = message }]
    };
}
