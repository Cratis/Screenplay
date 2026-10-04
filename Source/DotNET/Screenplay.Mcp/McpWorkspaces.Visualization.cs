// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object Visualize(JsonElement arguments)
    {
        McpRecoveryJournal.RefusePending(Root);
        var proposalId = McpJson.OptionalString(arguments, "proposalId");
        var hasSketch = arguments.TryGetProperty("sketch", out var sketch);
        if (proposalId is not null && hasSketch)
        {
            throw new McpFailure("Pass either proposalId or sketch: a board shows one change at a time.", -32602);
        }

        if (proposalId is not null)
        {
            var proposal = Proposal(arguments);
            return McpVisualization.Result(Root.ApplicationName, proposal.Before.Documents, proposal.Workspace.Documents, proposalId);
        }

        var documents = Root.Read(allowEmpty: true);
        return hasSketch
            ? McpVisualization.Result(Root.ApplicationName, documents, Sketch(documents, sketch), null)
            : McpVisualization.Result(Root.ApplicationName, documents, null, null);
    }

    static ImmutableArray<WorkspaceDocument> Sketch(ImmutableArray<WorkspaceDocument> documents, JsonElement sketch)
    {
        var sketched = documents.ToDictionary(document => document.Path.Value, StringComparer.Ordinal);
        foreach (var item in sketch.EnumerateArray())
        {
            McpJson.ValidateObject(item, ["path", "source"], ["path", "source"]);
            var path = McpJson.RequiredString(item, "path");
            if (!PortablePlayPath.TryParse(path, out var portable))
            {
                throw new McpFailure($"Sketch path '{path}' is not a relative .play path.", -32602);
            }

            var bytes = Encoding.UTF8.GetBytes(McpJson.RequiredString(item, "source"));
            sketched[portable.Value] = WorkspaceDocument.Create($"sketch:{portable.Value}", portable, bytes);
        }

        var result = sketched.Values.OrderBy(document => document.Path.Value, StringComparer.Ordinal).ToImmutableArray();
        McpRoot.CheckDocuments(result, allowEmpty: true);
        return result;
    }
}
