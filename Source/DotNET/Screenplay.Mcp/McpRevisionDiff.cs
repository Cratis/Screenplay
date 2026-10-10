// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpRevisionDiff
{
    internal static object Read(JsonElement arguments, McpWorkspaces? workspaces = null)
    {
        var before = Resolve(arguments, "before", workspaces);
        var after = Resolve(arguments, "after", workspaces);
        var matchByAddress = !before.HasPersistedIdentities || !after.HasPersistedIdentities;
        if (!matchByAddress && before.Workspace.IdentityCatalog.Application != after.Workspace.IdentityCatalog.Application)
        {
            throw new McpFailure("IncompatibleRevisions: snapshots must belong to the same application identity; identity continuity cannot be inferred across applications.") { FailureKind = "IncompatibleRevisions" };
        }

        return McpJson.ToolResult(McpSemanticDiff.Compare(before.Workspace, after.Workspace, arguments, matchByAddress));
    }

    static ComparisonSource Resolve(JsonElement arguments, string side, McpWorkspaces? workspaces)
    {
        var legacyName = $"{side}WorkspaceJson";
        if (!arguments.TryGetProperty(side, out var source))
        {
            return Restore(arguments, legacyName);
        }

        if (arguments.TryGetProperty(legacyName, out _))
        {
            throw new McpFailure($"Select either '{side}' or '{legacyName}', not both.", -32602);
        }

        McpJson.ValidateObject(source, ["workspace", "path", "workspaceJson"], []);
        if (source.EnumerateObject().Count() != 1)
        {
            throw new McpFailure($"'{side}' must select exactly one of workspace, path or workspaceJson.", -32602);
        }

        return Select(source, side, workspaces ?? new McpWorkspaces());
    }

    static ComparisonSource Select(JsonElement source, string side, McpWorkspaces workspaces)
    {
        if (source.TryGetProperty("workspaceJson", out _))
        {
            return Restore(source, "workspaceJson");
        }

        if (source.TryGetProperty("path", out _))
        {
            var path = workspaces.ReadComparisonPath(McpJson.RequiredString(source, "path"));

            return new(path.Workspace, path.HasPersistedIdentities);
        }

        if (McpJson.RequiredString(source, "workspace") != "active")
        {
            throw new McpFailure($"'{side}.workspace' must be 'active'.", -32602);
        }

        var active = workspaces.ReadComparisonWorkspace();

        return new(active.Workspace, active.HasPersistedIdentities);
    }

    static ComparisonSource Restore(JsonElement arguments, string name)
    {
        var json = McpJson.RequiredString(arguments, name);
        try
        {
            return new(McpWorkspaceTransport.Restore(json), true);
        }
        catch (Exception failure) when (failure is McpFailure or InvalidScreenplayWorkspace or InvalidSemanticContract or
            InvalidWorkspaceDocument or InvalidPortablePlayPath or JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new McpFailure($"UnreadableRevision: '{name}' must be a complete canonical export-workspace snapshot with a valid catalog and matching content revision. {failure.Message}") { FailureKind = "UnreadableRevision" };
        }
    }

    sealed record ComparisonSource(ScreenplayWorkspace Workspace, bool HasPersistedIdentities);
}
