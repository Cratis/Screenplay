// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Tool.Mcp;

sealed class McpTools
{
    readonly McpRoot _root;
    readonly McpWorkspaces _workspaces;

    internal McpTools(McpRoot root)
    {
        _root = root;
        _workspaces = new(root);
    }

    internal object Call(JsonElement parameters)
    {
        var name = McpJson.RequiredString(parameters, "name");
        var arguments = parameters.TryGetProperty("arguments", out var supplied) ? supplied : McpJson.Empty;
        McpToolCatalog.Validate(name, arguments);
        try
        {
            return name switch
            {
                "open-workspace" => _workspaces.Open(arguments),
                "propose" => _workspaces.Propose(arguments, false),
                "expand-layout" => _workspaces.Propose(arguments, true),
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

    object Read(string name, JsonElement arguments)
    {
        var documents = _root.Read();
        var snapshot = new McpSnapshot(documents);
        var compilation = snapshot.Compilation;
        var index = snapshot.Index;
        var value = name switch
        {
            "diagnostics" => new { compilation.Success, fileCount = documents.Length, compilation.Diagnostics },
            "merged-document" => new
            {
                compilation.Success,
                fileCount = documents.Length,
                source = compilation.Value is null ? null : new ScreenplayPrinter().Print(compilation.Value),
                syntax = compilation.Value,
                compilation.Diagnostics
            },
            "find-declaration" => new
            {
                compilation.Success,
                matches = index.Declarations.Where(declaration =>
                    (declaration.Name == McpJson.RequiredString(arguments, "name") || declaration.Address == McpJson.RequiredString(arguments, "name")) &&
                    (McpJson.OptionalString(arguments, "kind") is not { } kind || kind == declaration.Kind)),
                compilation.Diagnostics
            },
            "find-references" => McpReadResults.References(index, arguments),
            _ => McpReadResults.Describe(snapshot, documents.Length)
        };
        return McpJson.ToolResult(value);
    }
}
