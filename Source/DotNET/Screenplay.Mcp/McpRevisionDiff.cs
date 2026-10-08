// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpRevisionDiff
{
    internal static object Read(JsonElement arguments)
    {
        var before = Restore(arguments, "beforeWorkspaceJson");
        var after = Restore(arguments, "afterWorkspaceJson");
        if (before.IdentityCatalog.Application != after.IdentityCatalog.Application)
        {
            throw new McpFailure("IncompatibleRevisions: snapshots must belong to the same application identity; identity continuity cannot be inferred across applications.") { FailureKind = "IncompatibleRevisions" };
        }

        return McpJson.ToolResult(McpSemanticDiff.Compare(before, after, arguments));
    }

    static ScreenplayWorkspace Restore(JsonElement arguments, string name)
    {
        var json = McpJson.RequiredString(arguments, name);
        try
        {
            return McpWorkspaceTransport.Restore(json);
        }
        catch (Exception failure) when (failure is McpFailure or InvalidScreenplayWorkspace or InvalidSemanticContract or
            InvalidWorkspaceDocument or InvalidPortablePlayPath or JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new McpFailure($"UnreadableRevision: '{name}' must be a complete canonical export-workspace snapshot with a valid catalog and matching content revision. {failure.Message}") { FailureKind = "UnreadableRevision" };
        }
    }
}
