// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object ProposeRepair(JsonElement arguments)
    {
        var expectedEvidence = McpRepairEvidence.Expected(arguments);
        var pinned = McpJson.Boolean(arguments, "pinRepairEvidence");
        if (pinned != (expectedEvidence is not null))
        {
            throw new McpFailure("Pinned repair proposals require pinRepairEvidence=true and expectedRepairEvidenceRevision.", -32602);
        }

        var workspace = Current();
        McpRepairEvidence.Check(expectedEvidence, workspace);
        var expectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision"));
        var expectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision"));
        var formatting = McpJson.Enumeration(arguments, "formatting", WorkspaceAuthoringFormatting.PreserveExactSource);
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = expectedRevision,
            ExpectedCatalogRevision = expectedCatalogRevision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = formatting,
            AttachmentLoader = documents => McpAttachmentContents.Load(Root, documents)
        };
        if (expectedRevision != workspace.Revision || expectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return Rejected(workspace.ProposeAuthoring(request));
        }

        Root.Verify(workspace);
        var code = McpJson.RequiredString(arguments, "diagnosticCode");
        if (pinned && code is not ("PLAY0166" or "PLAY0478"))
        {
            throw new McpFailure("Pinned repair evidence v1 supports PLAY0166 and PLAY0478 only.", -32602) { FailureKind = "UnsupportedRepair" };
        }

        var subject = McpAstHandles.Read(arguments.GetProperty("subject"));
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, code, subject, request);
        if (result.Conflicts.Any(conflict => conflict.Kind == WorkspaceConflictKind.UnknownRepair))
        {
            throw new McpFailure("UnknownRepair: no unambiguous repair for this code and subject.", -32602) { FailureKind = "UnknownRepair" };
        }

        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation, request.ReferencePolicy), arguments) : Rejected(result);
    }

    internal object ProposeAst(JsonElement arguments, bool layout = false, bool source = false)
    {
        var workspace = Current();
        _ = McpJson.RequiredString(arguments, "formatting");
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision")),
            ExpectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision")),
            Validation = McpJson.Enumeration(arguments, "validation", WorkspaceAuthoringValidation.Authoring),
            Formatting = McpJson.Enumeration(arguments, "formatting", WorkspaceAuthoringFormatting.PreserveExactSource),
            ReferencePolicy = McpJson.Enumeration(arguments, "referencePolicy", WorkspaceAuthoringReferencePolicy.Safe),
            RelocatesCompositionComments = layout,
            AttachmentLoader = documents => McpAttachmentContents.Load(Root, documents)
        };
        if (request.ExpectedRevision != workspace.Revision || request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return Rejected(workspace.ProposeAuthoring(request));
        }

        Root.Verify(workspace);
        var index = McpWorkspaceAnalysis.For(workspace).Syntax;
        (ImmutableArray<WorkspaceAstOperation> Nodes, ImmutableArray<WorkspaceOperation> Documents) edits = ([], []);
        if (layout)
        {
            edits = LayoutOperations(workspace, index, McpJson.OptionalString(arguments, "layout") ?? "slice");
        }
        else if (!source)
        {
            edits = (McpAstOperations.Read(arguments, index), McpAstOperations.Documents(arguments));
        }

        if (source)
        {
            var parsed = McpSourceDocuments.Parse(workspace, McpSourceDocuments.Read(arguments));
            if (parsed.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                return McpJson.ToolResult(new { success = false, failureKind = "SourceParseFailed", authoringDiagnostics = parsed.Diagnostics }, true);
            }

            edits.Documents = parsed.Documents;
        }
        request = request with
        {
            Operations = edits.Nodes,
            Documents = edits.Documents,
            SemanticRenames = McpIdentityChanges.SemanticRenames(arguments),
            EventRenames = McpIdentityChanges.EventRenames(arguments),
            RetiredSemanticAddresses = McpIdentityChanges.RetiredSemanticAddresses(arguments),
            RetiredEventAddresses = McpIdentityChanges.RetiredEventAddresses(arguments)
        };
        var result = workspace.ProposeAuthoring(request);
        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation, request.ReferencePolicy), arguments, layout) : Rejected(result);
    }

    static object Rejected(WorkspaceAuthoringResult result)
    {
        var response = new
        {
            success = false,
            failureKind = result.Conflicts.FirstOrDefault()?.Kind switch
            {
                WorkspaceConflictKind.StaleWorkspaceRevision or WorkspaceConflictKind.StaleCatalogRevision => "StaleRevision",
                WorkspaceConflictKind.FormattingConsentRequired => "FormattingConsentRequired",
                WorkspaceConflictKind.UnknownRepair => "UnknownRepair",
                _ => "ProposalRejected"
            },
            result.Conflicts,
            identityMigrationIssues = result.Conflicts.SelectMany(conflict => conflict.IdentityMigrationIssues).Select(issue => issue.Describe()),
            result.AuthoringDiagnostics,
            result.ExecutableReady,
            result.ExecutableDiagnostics
        };
        return McpJson.ToolResult(response, true);
    }

    static (ImmutableArray<WorkspaceAstOperation>, ImmutableArray<WorkspaceOperation>) LayoutOperations(
        ScreenplayWorkspace workspace, WorkspaceSyntaxIndex index, string layout)
    {
        var nodes = ImmutableArray.CreateBuilder<WorkspaceAstOperation>();
        var documents = ImmutableArray.CreateBuilder<WorkspaceOperation>();
        var parsed = McpSourceDocuments.Parse(workspace, McpLayout.Expand(workspace, layout), generatedLayout: true);

        foreach (var operation in parsed.Documents)
        {
            switch (operation)
            {
                case ReplaceWorkspaceSyntaxDocument replace:
                    var entry = index.Entries.Single(item => item.Handle.Document == replace.Document && item.Handle.Path.Length == 0);
                    var original = workspace.Documents.Single(document => document.Id == replace.Document);
                    var syntax = replace.Syntax;
                    if (original.Path.Value == PlayFileWriter.RootFileName)
                    {
                        // The root's imported declarations and comments have been redistributed across
                        // documents. Do not restore its old import comments onto generated imports as well.
                        documents.Add(new ReplaceWorkspaceSyntaxDocument(original.Id, syntax));
                    }
                    else
                    {
                        nodes.Add(new ReplaceWorkspaceNode(entry.Handle, entry.Node, syntax));
                    }
                    break;
                case CreateWorkspaceSyntaxDocument create:
                    documents.Add(create);
                    break;
                default:
                    documents.Add(operation);
                    break;
            }
        }

        return (nodes.ToImmutable(), documents.ToImmutable());
    }
}
