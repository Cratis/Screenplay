// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

public static partial class WorkspaceEventRefactorings
{
    /// <summary>
    /// Moves a standalone event declaration into its single producing <c>produces</c> clause as an inline event.
    /// Requires explicit canonical formatting consent and refuses, leaving the source unchanged, unless meaning is provably preserved.
    /// </summary>
    /// <param name="workspace">The original workspace.</param>
    /// <param name="subject">The original standalone EventSyntax handle.</param>
    /// <param name="request">The revision-bound authoring request with formatting consent.</param>
    /// <returns>A complete proposal or typed conflicts without a partial candidate.</returns>
    public static WorkspaceAuthoringResult ProposeInlineStandaloneEvent(ScreenplayWorkspace workspace, WorkspaceNodeHandle subject, WorkspaceAuthoringRequest request)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(request);
        if (request.ExpectedRevision != workspace.Revision || request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return WorkspaceRepairVerification.Propose(workspace, request);
        }

        if (request.Formatting != WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            return Refuse(WorkspaceConflictKind.FormattingConsentRequired, "Inlining an event requires explicit CanonicalizeTouchedDocuments consent.");
        }

        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (index.Find(subject) is not { Node: EventSyntax } declaration)
        {
            return Refuse(WorkspaceConflictKind.InvalidOperation, "The subject must be a standalone event declaration in the current workspace.");
        }

        var eligibility = WorkspaceEventInlineEligibility.Check(index, declaration);
        if (eligibility.Refusal is { } refusal)
        {
            return Refuse(WorkspaceConflictKind.InvalidOperation, $"Cannot inline event '{((EventSyntax)declaration.Node).Name}': {refusal}");
        }

        var production = eligibility.Production!;
        ImmutableArray<WorkspaceAstOperation> operations = [new MoveWorkspaceNode(declaration.Handle, declaration.Node, production.Handle, production.Node, "inlineEvent")];
        var result = WorkspaceRepairVerification.RequireComments(WorkspaceRepairVerification.Propose(workspace, request with { Operations = operations }));
        if (result.Accepted && (!WorkspaceRepairVerification.SameModel(workspace, result.Workspace!) ||
            workspace.IdentityCatalog.Revision != result.Workspace!.IdentityCatalog.Revision ||
            result.Workspace.Compilation.Diagnostics.Count() != workspace.Compilation.Diagnostics.Count()))
        {
            return WorkspaceRepairVerification.Refuse(result, WorkspaceConflictKind.InvalidOperation, "Inlining must preserve canonical executable bytes, every catalog assignment and the diagnostics of the model.");
        }

        return result;
    }
}
