// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
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

    internal object ProposeAst(JsonElement arguments, bool layout = false)
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
            AttachmentLoader = documents => McpAttachmentContents.Load(Root, documents)
        };
        if (request.ExpectedRevision != workspace.Revision || request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return Rejected(workspace.ProposeAuthoring(request));
        }

        Root.Verify(workspace);
        var index = McpWorkspaceAnalysis.For(workspace).Syntax;
        var edits = layout ? LayoutOperations(workspace, index, McpJson.OptionalString(arguments, "layout") ?? "slice")
            : (McpAstOperations.Read(arguments, index), McpAstOperations.Documents(arguments));
        request = request with
        {
            Operations = edits.Item1,
            Documents = edits.Item2,
            SemanticRenames = McpIdentityChanges.SemanticRenames(arguments),
            EventRenames = McpIdentityChanges.EventRenames(arguments),
            RetiredSemanticAddresses = McpIdentityChanges.RetiredSemanticAddresses(arguments),
            RetiredEventAddresses = McpIdentityChanges.RetiredEventAddresses(arguments)
        };
        var result = workspace.ProposeAuthoring(request);
        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation, request.ReferencePolicy), arguments) : Rejected(result);
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
        var operations = McpLayout.Expand(workspace, layout);
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var operation in operations)
        {
            if (operation is ReplaceWorkspaceDocument replace)
            {
                var original = workspace.Documents.Single(document => document.Id == replace.Document);
                sources.Add(original.Path.Value, WorkspaceDocument.Create(original.Id, original.StableKey, original.Path, replace.Bytes.AsSpan()).Text);
            }
            else if (operation is AddWorkspaceDocument add)
            {
                sources.Add(add.Path.Value, WorkspaceDocument.Create(add.StableKey, add.Path, add.Bytes.AsSpan()).Text);
            }
        }

        var (placed, diagnostics) = PlayImports.Resolve(sources.Keys, new InMemoryPlayDocumentSource(sources));
        if (diagnostics.Any(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error))
        {
            throw new McpFailure("Generated layout imports could not be resolved.");
        }

        var placements = placed.ToDictionary(document => document.Path, document => document.Placement, StringComparer.Ordinal);
        foreach (var operation in operations)
        {
            switch (operation)
            {
                case ReplaceWorkspaceDocument replace:
                    var entry = index.Entries.Single(item => item.Handle.Document == replace.Document && item.Handle.Path.Length == 0);
                    var original = workspace.Documents.Single(document => document.Id == replace.Document);
                    var syntax = Parse(WorkspaceDocument.Create(original.Id, original.StableKey, original.Path, replace.Bytes.AsSpan()), placements[original.Path.Value]);
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
                case AddWorkspaceDocument add:
                    var document = WorkspaceDocument.Create(add.StableKey, add.Path, add.Bytes.AsSpan());
                    documents.Add(new CreateWorkspaceSyntaxDocument(add.StableKey, add.Path, Parse(document, placements[document.Path.Value]), document.Encoding));
                    break;
                default:
                    documents.Add(operation);
                    break;
            }
        }

        return (nodes.ToImmutable(), documents.ToImmutable());
    }

    static ApplicationSyntax Parse(WorkspaceDocument document, PlayPlacement placement)
    {
        var parsed = new ScreenplayCompiler().Parse(document.Text, document.Path.Value, placement);
        return parsed.Success ? parsed.Value! : throw new McpFailure($"Generated layout document '{document.Path}' could not be parsed.");
    }
}
