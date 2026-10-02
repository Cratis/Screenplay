// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Provides explicit event refactorings through revision-bound typed authoring proposals.
/// </summary>
public static class WorkspaceEventRefactorings
{
    /// <summary>
    /// Moves an inline event declaration into its owning slice, retaining routing, metadata and assigned identities.
    /// Requires explicit canonical formatting consent and refuses any comment or executable-model change.
    /// </summary>
    /// <param name="workspace">The original workspace.</param>
    /// <param name="subject">The original inline EventSyntax handle.</param>
    /// <param name="request">The revision-bound authoring request with formatting consent.</param>
    /// <returns>A complete proposal or typed conflicts without a partial candidate.</returns>
    public static WorkspaceAuthoringResult ProposeExtractInlineEvent(ScreenplayWorkspace workspace, WorkspaceNodeHandle subject, WorkspaceAuthoringRequest request)
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
            return Refuse(WorkspaceConflictKind.FormattingConsentRequired, "Extracting an inline event requires explicit CanonicalizeTouchedDocuments consent.");
        }

        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (index.Find(subject) is not { Node: EventSyntax } declaration || declaration.Parent is null ||
            index.Find(declaration.Parent) is not { Node: ProducesSyntax { InlineEvent: not null } produces } production ||
            index.Find(production.Parent!) is not { Node: CommandSyntax command } commandEntry ||
            index.Find(commandEntry.Parent!) is not { Node: SliceSyntax } slice)
        {
            return Refuse(WorkspaceConflictKind.InvalidOperation, "The subject must be an inline event declaration in the current workspace.");
        }

        var operations = ImmutableArray.CreateBuilder<WorkspaceAstOperation>();
        operations.Add(new MoveWorkspaceNode(declaration.Handle, declaration.Node, slice.Handle, slice.Node, "events"));
        if (produces.For is null)
        {
            var identifiers = command.Properties.Where(property => property.IsIdentifier && !property.Type.IsOptional && !property.Type.IsCollection).ToArray();
            if (identifiers.Length != 1)
            {
                return Refuse(WorkspaceConflictKind.InvalidOperation, "The inline production has no unique required scalar identifier destination.");
            }

            operations.Add(new AddWorkspaceNode(production.Handle, produces, "for", new PathExpressionSyntax(identifiers[0].Name, produces.Location)));
        }

        var result = WorkspaceRepairVerification.RequireComments(WorkspaceRepairVerification.Propose(workspace, request with { Operations = operations.ToImmutable() }));
        if (result.Accepted && (!WorkspaceRepairVerification.SameModel(workspace, result.Workspace!) ||
            workspace.IdentityCatalog.Revision != result.Workspace!.IdentityCatalog.Revision))
        {
            return WorkspaceRepairVerification.Refuse(result, WorkspaceConflictKind.InvalidOperation, "Extraction must preserve canonical executable bytes and every catalog assignment.");
        }

        if (result.Accepted && !SameComments(result.WritePlan!))
        {
            return WorkspaceRepairVerification.Refuse(result, WorkspaceConflictKind.RepairWouldDropComments, "Extraction must preserve each comment exactly once.");
        }

        return result;
    }

    static bool SameComments(WorkspaceWritePlan plan)
    {
        static IEnumerable<string> Comments(IEnumerable<WorkspaceDocument> documents) => documents
            .SelectMany(document => WorkspaceSourceTokenizer.Tokenize(document).Tokens)
            .Where(token => token.Kind == WorkspaceSourceTokenKind.Comment).Select(token => token.Text).Order(StringComparer.Ordinal);
        return Comments(plan.Entries.Select(entry => entry.Before).OfType<WorkspaceDocument>())
            .SequenceEqual(Comments(plan.Entries.Select(entry => entry.After).OfType<WorkspaceDocument>()));
    }

    static WorkspaceAuthoringResult Refuse(WorkspaceConflictKind kind, string message) => new()
    {
        Conflicts = [new WorkspaceConflict { Kind = kind, Message = message }]
    };
}
