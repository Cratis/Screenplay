// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

internal sealed partial class McpWorkspaces
{
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
            ReferencePolicy = McpJson.Enumeration(arguments, "referencePolicy", WorkspaceAuthoringReferencePolicy.Safe)
        };
        if (request.ExpectedRevision != workspace.Revision || request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return Rejected(workspace.ProposeAuthoring(request));
        }

        root.Verify(workspace);
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
        foreach (var operation in McpLayout.Expand(workspace, layout))
        {
            switch (operation)
            {
                case ReplaceWorkspaceDocument replace:
                    var entry = index.Entries.Single(item => item.Handle.Document == replace.Document && item.Handle.Path.Length == 0);
                    var original = workspace.Documents.Single(document => document.Id == replace.Document);
                    nodes.Add(new ReplaceWorkspaceNode(entry.Handle, entry.Node, Parse(WorkspaceDocument.Create(original.Id, original.StableKey, original.Path, replace.Bytes.AsSpan()))));
                    break;
                case AddWorkspaceDocument add:
                    var document = WorkspaceDocument.Create(add.StableKey, add.Path, add.Bytes.AsSpan());
                    documents.Add(new CreateWorkspaceSyntaxDocument(add.StableKey, add.Path, Parse(document), document.Encoding));
                    break;
                default:
                    documents.Add(operation);
                    break;
            }
        }

        return (nodes.ToImmutable(), documents.ToImmutable());
    }

    static ApplicationSyntax Parse(WorkspaceDocument document)
    {
        var parsed = new ScreenplayCompiler().Parse(document.Text, document.Path.Value);
        return parsed.Success ? parsed.Value! : throw new McpFailure($"Generated layout document '{document.Path}' could not be parsed.");
    }
}
