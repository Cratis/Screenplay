// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object NumberEnumMembers(JsonElement arguments)
    {
        var workspace = Current();
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision")),
            ExpectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision")),
            Formatting = McpJson.Enumeration(arguments, "formatting", WorkspaceAuthoringFormatting.PreserveExactSource),
            Validation = McpJson.Enumeration(arguments, "validation", WorkspaceAuthoringValidation.Authoring)
        };
        Root.Verify(workspace);
        var result = WorkspaceEnumerationRefactorings.ProposeNumberMembers(workspace, McpAstHandles.Read(arguments.GetProperty("subject")), request);
        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation), arguments) : Rejected(result);
    }

    internal object ExtractInlineEvent(JsonElement arguments)
    {
        var workspace = Current();
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision")),
            ExpectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision")),
            Formatting = McpJson.Enumeration(arguments, "formatting", WorkspaceAuthoringFormatting.PreserveExactSource),
            Validation = McpJson.Enumeration(arguments, "validation", WorkspaceAuthoringValidation.Authoring)
        };
        Root.Verify(workspace);
        var result = WorkspaceEventRefactorings.ProposeExtractInlineEvent(workspace, McpAstHandles.Read(arguments.GetProperty("subject")), request);
        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation), arguments) : Rejected(result);
    }

    internal object Move(JsonElement arguments)
    {
        var workspace = Current();
        var request = new WorkspaceMoveRequest
        {
            ExpectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision")),
            ExpectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision")),
            Target = McpSemanticAddresses.Read(arguments.GetProperty("target")),
            NewParent = McpSemanticAddresses.Read(arguments.GetProperty("newParent")),
            TargetHandle = arguments.TryGetProperty("targetHandle", out var target) ? McpAstHandles.Read(target) : null,
            NewParentHandle = arguments.TryGetProperty("newParentHandle", out var parent) ? McpAstHandles.Read(parent) : null,
            Formatting = McpJson.Enumeration(arguments, "formatting", WorkspaceAuthoringFormatting.PreserveTrivia),
            Validation = McpJson.Enumeration(arguments, "validation", WorkspaceAuthoringValidation.Authoring)
        };
        Root.Verify(workspace);
        if (!McpDeclarationMove.Applies(request.Target, request.NewParent))
        {
            var moved = workspace.ProposeMove(request);
            return moved.Accepted ? Store(new McpAuthoringProposal(workspace, moved, request.Validation), arguments) : Rejected(moved);
        }
        var before = McpWorkspaceAnalysis.For(workspace).Source;
        var (carried, refusal) = McpDeclarationMove.Carried(before, request.Target);
        if (refusal is not null) return MoveRefused(request, refusal);
        var result = workspace.ProposeMove(request with { CarriedSpecifications = carried });
        if (!result.Accepted) return Rejected(result);
        refusal = McpDeclarationMove.Obligations(before, McpWorkspaceAnalysis.For(result.Workspace!).Source, result.MoveReport!.AddressMigrations);
        return refusal is null ? Store(new McpAuthoringProposal(workspace, result, request.Validation), arguments) : MoveRefused(request, refusal);
    }

    static object MoveRefused(WorkspaceMoveRequest request, string reason) => McpJson.ToolResult(new
    {
        success = false,
        failureKind = "InvalidOperation",
        conflicts = new[] { new { kind = nameof(WorkspaceConflictKind.InvalidOperation), message = $"Move '{MovePath(request.Target)}' to '{MovePath(request.NewParent)}' refused: {reason}" } }
    }, true);

    static string MovePath(SemanticAddress address) => $"{address.Kind}:{string.Join('.', address.Parts.Skip(1).Select(part => part.Key))}";

    internal object Rename(JsonElement arguments)
    {
        var workspace = Current();
        var request = new WorkspaceRenameRequest
        {
            ExpectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision")),
            ExpectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision")),
            Target = arguments.TryGetProperty("target", out var target) ? McpAstHandles.Read(target) : throw new McpFailure("Rename requires a target handle.", -32602),
            ExpectedName = McpJson.RequiredString(arguments, "expectedName"),
            NewName = McpJson.RequiredString(arguments, "newName"),
            EventNeverPersisted = McpJson.Boolean(arguments, "eventNeverPersisted"),
            ReleaseBaseline = _planningBaseline?.Read,
            Formatting = McpJson.Enumeration(arguments, "formatting", WorkspaceAuthoringFormatting.PreserveTrivia),
            Validation = McpJson.Enumeration(arguments, "validation", WorkspaceAuthoringValidation.Authoring)
        };
        Root.Verify(workspace);
        var result = workspace.ProposeRename(request);
        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation), arguments) : Rejected(result);
    }

    // Retain the response member for existing clients; event pins are carried by ESM v16.
    static IEnumerable<Diagnostic> EventPinWarnings(ScreenplayWorkspace workspace) => [];
}
