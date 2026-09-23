// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Mcp;

sealed class McpTools
{
    readonly McpRoot _root;
    readonly McpWorkspaces _workspaces;
    readonly McpSourceCache _sources = new();

    internal McpTools(McpRoot root)
    {
        _root = root;
        _workspaces = new(root);
    }

    internal object Call(JsonElement parameters)
    {
        McpJson.ValidateObject(parameters, ["name", "arguments", "_meta"], ["name"]);
        var name = McpJson.RequiredString(parameters, "name");
        var arguments = parameters.TryGetProperty("arguments", out var supplied) ? supplied : McpJson.Empty;
        McpToolCatalog.Validate(name, arguments);
        try
        {
            return name switch
            {
                "syntax-schema" => Schema(arguments),
                "open-workspace" => _workspaces.Open(arguments),
                "workspace-state" => _workspaces.State(arguments),
                "recover-workspace" => _workspaces.Recover(arguments),
                "propose-rename" => _workspaces.Rename(arguments),
                "read-workspace" => _workspaces.ReadWorkspace(arguments),
                "read-ast" => _workspaces.ReadAst(arguments),
                "propose" => _workspaces.Propose(arguments, false),
                "propose-ast" => _workspaces.ProposeAst(arguments),
                "expand-layout" => McpJson.OptionalString(arguments, "validation") == "Authoring" ? _workspaces.ProposeAst(arguments, true) : _workspaces.Propose(arguments, true),
                "read-proposal" => _workspaces.ReadProposal(arguments),
                "export-workspace" => _workspaces.ExportWorkspace(arguments),
                "discard-proposal" => _workspaces.DiscardProposal(arguments),
                "apply" => _workspaces.Apply(arguments),
                _ => Read(name, arguments)
            };
        }
        catch (McpFailure failure) when (failure.Code != 0)
        {
            throw;
        }
        catch (Exception exception)
        {
            return McpJson.ToolResult(new { success = false, error = exception.GetType().Name, message = exception.Message }, true);
        }
    }

    static object Schema(JsonElement arguments) => McpJson.ToolResult(McpJson.OptionalString(arguments, "kind") is { } kind
        ? SyntaxSchema.For(kind)
        : McpPaging.Page(SyntaxSchema.Kinds, arguments, $"syntax:{typeof(SyntaxJson).Assembly.GetName().Version}"));

    object Read(string name, JsonElement arguments)
    {
        McpRecoveryJournal.RefusePending(_root);
        var documents = _root.Read();
        var snapshot = _sources.Read(documents);
        McpRecoveryJournal.RefusePending(_root);
        var expectedSource = McpJson.OptionalString(arguments, "expectedSourceRevision");
        if (McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 && expectedSource is null)
        {
            throw new McpFailure("Subsequent source pages require expectedSourceRevision from the first page.", -32602);
        }

        if (expectedSource is not null && expectedSource != snapshot.SourceRevision)
        {
            throw new McpFailure("StaleSourceRevision: source changed between read pages; start a new query.");
        }

        var value = name switch
        {
            "diagnostics" => McpModelQueries.Diagnostics(snapshot, documents.Length, arguments),
            "merged-document" => McpDocumentReads.Merged(snapshot, documents.Length, arguments),
            "read-document" => McpDocumentReads.Original(snapshot, documents, arguments),
            "search-declarations" => McpModelQueries.Search(snapshot, arguments),
            "declaration-details" => McpDeclarationDetails.Read(snapshot, arguments),
            "dependencies" => McpDependencyQueries.Read(snapshot, arguments),
            "find-declaration" => McpModelQueries.Find(snapshot, arguments),
            "find-references" => McpDependencyQueries.Legacy(snapshot, arguments),
            "recommend-layout" => McpLayoutRecommendation.Describe(snapshot, documents),
            "find-fixtures" => McpFixtureQueries.Values(
                snapshot,
                McpJson.OptionalString(arguments, "specification"),
                McpJson.OptionalString(arguments, "role"),
                McpJson.OptionalString(arguments, "property"),
                McpJson.OptionalString(arguments, "value"),
                McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue),
                McpJson.Integer(arguments, "limit", 50, 1, 200),
                McpJson.OptionalString(arguments, "scope"),
                McpJson.OptionalString(arguments, "document")),
            "find-assertion-gaps" => McpFixtureQueries.AssertionGaps(
                snapshot,
                McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue),
                McpJson.Integer(arguments, "limit", 50, 1, 200),
                McpJson.OptionalString(arguments, "scope"),
                McpJson.OptionalString(arguments, "document")),
            _ => McpModelQueries.Describe(snapshot, documents.Length, arguments)
        };
        var result = McpJson.ToolResult(value);
        McpRecoveryJournal.RefusePending(_root);
        return result;
    }
}
