// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Provides opt-in pinning of positional enumeration member identity.
/// </summary>
public static class WorkspaceEnumerationRefactorings
{
    /// <summary>
    /// Proposes numbering every member of a wholly unnumbered enum by its current zero-based position.
    /// Never renumbers numbered or parser-invalid enums, and never writes before explicit apply.
    /// </summary>
    /// <param name="workspace">The original workspace.</param>
    /// <param name="subject">The revision-bound concept occurrence.</param>
    /// <param name="request">The authoring request with explicit canonical formatting consent.</param>
    /// <returns>A complete typed proposal or conflicts.</returns>
    public static WorkspaceAuthoringResult ProposeNumberMembers(ScreenplayWorkspace workspace, WorkspaceNodeHandle subject, WorkspaceAuthoringRequest request)
    {
        if (request.ExpectedRevision != workspace.Revision || request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return WorkspaceRepairVerification.Propose(workspace, request);
        }

        if (request.Formatting != WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            return Refuse(WorkspaceConflictKind.FormattingConsentRequired, "Numbering enum members requires explicit CanonicalizeTouchedDocuments consent.");
        }

        var entry = WorkspaceSyntaxIndex.Create(workspace).Find(subject);
        if (entry?.Node is not ConceptSyntax { IsEnum: true, ValueNumbers: null } concept || !concept.Values.Any() ||
            workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.MixedEnumerationNumbering || diagnostic.Code == DiagnosticCodes.DuplicateEnumerationNumber || diagnostic.Code == DiagnosticCodes.EnumerationNumberOutsideInt32 || diagnostic.Code == DiagnosticCodes.MalformedEnumerationNumber))
        {
            return Refuse(WorkspaceConflictKind.InvalidOperation, "The subject must be a wholly unnumbered enum with members; numbered, mixed and invalid enums cannot be renumbered.");
        }

        var result = WorkspaceRepairVerification.RequireComments(WorkspaceRepairVerification.Propose(workspace, request with
        {
            Operations = [new ReplaceWorkspaceNode(entry.Handle, concept, concept with { ValueNumbers = [.. Enumerable.Range(0, concept.Values.Count())] })]
        }));
        if (result.Accepted && (!WorkspaceRepairVerification.KeepsCatalog(workspace, result.Workspace!) ||
            (workspace.Compilation.Value is not null && !WorkspaceRepairVerification.SameModel(workspace, result.Workspace!))))
        {
            return WorkspaceRepairVerification.Refuse(result, WorkspaceConflictKind.InvalidOperation, "Numbering positional enum members must preserve executable bytes and assigned identities.");
        }

        return result;
    }

    static WorkspaceAuthoringResult Refuse(WorkspaceConflictKind kind, string message) => new()
    {
        Conflicts = [new WorkspaceConflict { Kind = kind, Message = message }]
    };
}
