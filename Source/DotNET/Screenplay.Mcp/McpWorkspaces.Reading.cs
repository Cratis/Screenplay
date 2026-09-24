// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object ReadWorkspace(JsonElement arguments)
    {
        var workspace = CheckedCurrent(arguments);
        var view = McpJson.OptionalString(arguments, "view") ?? "documents";
        var items = view switch
        {
            "documents" => workspace.Documents.Select(document => (object)new
            {
                documentId = document.Id.ToString(),
                path = document.Path.Value,
                document.StableKey,
                document.Encoding,
                byteCount = document.Bytes.Length,
                root = McpAstHandles.Describe(new(workspace.Revision, document.Id, string.Empty))
            }),
            "semantics" => workspace.IdentityCatalog.Semantics.Select(assignment => (object)new
            {
                semanticId = assignment.Id.ToString(),
                address = McpSemanticAddresses.Describe(assignment.Address),
                assignment.Origin
            }),
            "eventContracts" => workspace.IdentityCatalog.EventContracts.Select(assignment => (object)new
            {
                eventContractId = assignment.Id.ToString(),
                address = McpSemanticAddresses.Describe(assignment.Address),
                revision = assignment.Revision.ToString(),
                assignment.Origin
            }),
            "diagnostics" => McpWorkspaceAnalysis.For(workspace).Source.Compilation.Diagnostics,
            "executable-diagnostics" => workspace.Compilation.Diagnostics,
            "implementation-requirements" => workspace.Compilation.ImplementationRequirements.Select(DescribeRequirement),
            _ => throw new McpFailure("Unknown workspace view.", -32602)
        };
        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace),
            view,
            page = McpPaging.Page(items, arguments, workspace.Revision.ToString())
        });
    }

    internal object ReadAst(JsonElement arguments)
    {
        var workspace = CheckedCurrent(arguments);
        var analysis = McpWorkspaceAnalysis.For(workspace);
        var index = analysis.Syntax;
        var documentId = McpJson.OptionalString(arguments, "documentId");
        var path = McpJson.OptionalString(arguments, "path");
        var includeContent = McpJson.Boolean(arguments, "includeContent");
        var view = McpJson.OptionalString(arguments, "view") ?? "nodes";
        var candidates = index.Entries.AsEnumerable();
        if (documentId is not null)
        {
            var id = DocumentId.Parse(documentId);
            if (!workspace.Documents.Any(document => document.Id == id))
            {
                throw new McpFailure("UnknownDocument: the document is not in this workspace.");
            }

            candidates = candidates.Where(entry => entry.Handle.Document == id);
        }

        if (path is not null && documentId is null)
        {
            throw new McpFailure("A node path requires documentId.", -32602);
        }

        candidates = view switch
        {
            "nodes" => path is null ? candidates : candidates.Where(entry => entry.Handle.Path == path),
            "children" => path is null ? throw new McpFailure("Children view requires a parent path.", -32602) : candidates.Where(entry => entry.Parent?.Path == path),
            _ => throw new McpFailure("AST view must be nodes or children.", -32602)
        };
        if (McpJson.OptionalString(arguments, "kind") is { } kind)
        {
            candidates = candidates.Where(entry => entry.Kind == kind);
        }

        if (McpJson.OptionalString(arguments, "name") is { } name)
        {
            candidates = candidates.Where(entry => (entry.Node.GetType().GetProperty("Name")?.GetValue(entry.Node) as string) == name);
        }

        if (McpJson.OptionalString(arguments, "semanticId") is { } semanticId)
        {
            var id = SemanticId.Parse(semanticId);
            candidates = candidates.Where(entry => entry.SemanticId == id);
        }

        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace),
            index.Diagnostics,
            page = McpPaging.Page(candidates, entry => McpAstHandles.Describe(entry, includeContent, analysis.ChildCount(entry.Handle)), arguments, workspace.Revision.ToString())
        });
    }

    internal object ReadProposal(JsonElement arguments)
    {
        var proposal = Proposal(arguments);
        var view = McpJson.OptionalString(arguments, "view") ?? "changes";
        var result = view switch
        {
            "changes" => McpPaging.Page(
                proposal.WritePlan.Entries.Select(entry => new
                {
                    entry.Kind,
                    documentId = entry.Document.ToString(),
                    beforePath = entry.Before?.Path.Value,
                    afterPath = entry.After?.Path.Value,
                    beforeBytes = entry.Before?.Bytes.Length,
                    afterBytes = entry.After?.Bytes.Length
                }),
                arguments,
                proposal.Workspace.Revision.ToString()),
            "diagnostics" => McpPaging.Page(proposal is McpAuthoringProposal authoring ? authoring.Result.AuthoringDiagnostics : [], arguments, proposal.Workspace.Revision.ToString()),
            "executable-diagnostics" => McpPaging.Page(proposal.Workspace.Compilation.Diagnostics, arguments, proposal.Workspace.Revision.ToString()),
            "implementation-requirements" => McpPaging.Page(proposal.Workspace.Compilation.ImplementationRequirements, DescribeRequirement, arguments, proposal.Workspace.Revision.ToString()),
            "dropped-comments" => McpPaging.Page(
                WorkspaceDroppedComments.In(proposal.WritePlan).Select(comment => new
                {
                    documentId = comment.Document.ToString(),
                    path = comment.Path.Value,
                    comment.Line,
                    comment.Column,
                    comment.Text
                }),
                arguments,
                proposal.Workspace.Revision.ToString()),
            "before" or "after" => ProposalBytes(proposal, arguments, view),
            _ => throw new McpFailure("Unknown proposal view.", -32602)
        };
        return McpJson.ToolResult(new { proposal.Validation, before = McpWorkspaceTransport.Describe(proposal.Before), after = McpWorkspaceTransport.Describe(proposal.Workspace), view, result });
    }

    internal object ExportWorkspace(JsonElement arguments)
    {
        var workspace = McpJson.OptionalString(arguments, "proposalId") is null ? CheckedCurrent(arguments) : Proposal(arguments).Workspace;
        if (McpJson.RequiredString(arguments, "expectedRevision") != workspace.Revision.ToString())
        {
            throw new McpFailure("StaleRevision: export chunks must refer to one immutable workspace.");
        }

        return McpJson.ToolResult(McpPaging.Bytes(McpWorkspaceTransport.ExportBytes(workspace), arguments, workspace.Revision.ToString()));
    }

    internal object DiscardProposal(JsonElement arguments)
    {
        _ = Proposal(arguments);
        _proposals.Remove(McpJson.RequiredString(arguments, "proposalId"));
        return McpJson.ToolResult(new { discarded = true, remainingCount = _proposals.Count });
    }

    static object DescribeRequirement(SemanticImplementationRequirement requirement) => new
    {
        role = requirement.Role.ToString(),
        requirement.RequirementId,
        requirement.ContextVersion,
        requirement.ResultVersion,
        requirement.RequiredCapability,
        attachmentResolution = requirement.AttachmentResolution.ToString(),
        owner = McpSemanticAddresses.Describe(requirement.Owner),
        requirement.Member,
        requirement.Language,
        requirement.File,
        requirement.ContentHash,
        bodySpan = requirement.BodySpan,
        bodyLines = requirement.BodyLines.Select(position => new { line = position.Line, column = position.Column }),
        semanticId = requirement.Source.SemanticId.ToString(),
        documentId = requirement.Source.Span.Document.ToString(),
        line = requirement.Source.Span.StartLine,
        column = requirement.Source.Span.StartColumn
    };

    static object ProposalBytes(IMcpProposal proposal, JsonElement arguments, string view)
    {
        var id = DocumentId.Parse(McpJson.RequiredString(arguments, "documentId"));
        var entry = proposal.WritePlan.Entries.SingleOrDefault(entry => entry.Document == id)
            ?? throw new McpFailure("The document is not changed by this proposal.");
        var document = view == "before" ? entry.Before : entry.After;
        if (document is null)
        {
            return new { exists = false };
        }

        return new
        {
            exists = true,
            path = document.Path.Value,
            content = McpPaging.Bytes([.. document.Bytes], arguments, view == "before" ? proposal.Before.Revision.ToString() : proposal.Workspace.Revision.ToString())
        };
    }
}
