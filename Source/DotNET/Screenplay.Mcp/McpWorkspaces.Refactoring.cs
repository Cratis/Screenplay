// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
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
        var result = workspace.ProposeMove(request);
        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation), arguments) : Rejected(result);
    }

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
            Formatting = McpJson.Enumeration(arguments, "formatting", WorkspaceAuthoringFormatting.PreserveTrivia),
            Validation = McpJson.Enumeration(arguments, "validation", WorkspaceAuthoringValidation.Authoring)
        };
        Root.Verify(workspace);
        var result = workspace.ProposeRename(request);
        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation), arguments) : Rejected(result);
    }

    // Authoring proposals may be accepted even when unrelated syntax prevents semantic binding.
    static IEnumerable<Diagnostic> EventPinWarnings(ScreenplayWorkspace workspace) =>
        McpWorkspaceAnalysis.For(workspace).Syntax.Entries.Select(entry => entry.Node).OfType<EventSyntax>()
            .Where(declaration => declaration.Id is not null)
            .Select(declaration => new Diagnostic(
                DiagnosticSeverity.Warning,
                DiagnosticCodes.EventIdPinNotCarriedToRenderers,
                $"Event '{declaration.Name}' id pin '{declaration.Id}' is not yet carried to renderers; generated code uses the new name '{declaration.Name}'.",
                declaration.Location));
}
