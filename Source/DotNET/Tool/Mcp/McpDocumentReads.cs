// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpDocumentReads
{
    static readonly ConditionalWeakTable<McpSnapshot, byte[]> _merged = [];

    internal static object Original(McpSnapshot snapshot, ImmutableArray<WorkspaceDocument> documents, JsonElement arguments)
    {
        var path = PortablePlayPath.Parse(McpJson.RequiredString(arguments, "path"));
        var document = documents.SingleOrDefault(document => document.Path == path)
            ?? throw new McpFailure("UnknownDocument: the source path is not in this snapshot.");
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            path = document.Path.Value,
            document.Encoding,
            content = McpPaging.Bytes([.. document.Bytes], arguments, snapshot.SourceRevision)
        };
    }

    internal static object Merged(McpSnapshot snapshot, int fileCount, JsonElement arguments)
    {
        var view = McpJson.OptionalString(arguments, "view") ?? "both";
        if (snapshot.Compilation.Value is null)
        {
            return new { success = false, snapshot.SourceRevision, fileCount, diagnostics = McpModelQueries.DiagnosticSummary(snapshot) };
        }

        if (view == "source")
        {
            var bytes = _merged.GetValue(snapshot, value => Encoding.UTF8.GetBytes(new ScreenplayPrinter().Print(value.Compilation.Value!)));
            return new
            {
                snapshot.Compilation.Success,
                snapshot.SourceRevision,
                fileCount,
                canonicalized = true,
                content = McpPaging.Bytes(bytes, arguments, snapshot.SourceRevision)
            };
        }

        if (view != "syntax" && view != "both")
        {
            throw new McpFailure("Merged view must be source, syntax, or both.", -32602);
        }

        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            fileCount,
            source = view == "both" ? Encoding.UTF8.GetString(_merged.GetValue(snapshot, value => Encoding.UTF8.GetBytes(new ScreenplayPrinter().Print(value.Compilation.Value!)))) : null,
            syntax = snapshot.Compilation.Value,
            diagnostics = McpModelQueries.DiagnosticSummary(snapshot)
        };
    }
}
