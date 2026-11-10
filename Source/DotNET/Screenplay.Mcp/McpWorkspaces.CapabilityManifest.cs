// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Agents;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    static object ReadCapabilityManifest(ScreenplayWorkspace workspace, JsonElement arguments, string view)
    {
        var emission = AgentCapabilityManifest.From(workspace.Compilation);
        if (emission.Manifest is not { } manifest)
        {
            if (McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 || McpJson.OptionalString(arguments, "expectedModelRevision") is not null)
            {
                throw new McpFailure("StaleRevision: capability manifest is no longer available.");
            }

            return McpJson.ToolResult(new
            {
                workspace = McpWorkspaceTransport.Describe(workspace),
                view,
                available = false,
                refusal = emission.Refusal.ToString(),
                operation = emission.Operation,
                executableDiagnosticsCount = workspace.Compilation.Diagnostics.Count(),
                executableDiagnosticsView = "executable-diagnostics"
            });
        }

        var modelRevision = workspace.Compilation.Value!.Model.Revision.ToString();
        CheckContinuation(arguments, "expectedModelRevision", modelRevision, requireOnContinuation: true);
        var bytes = AgentCapabilityManifestJson.Write(manifest);
        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace),
            view,
            available = true,
            format = AgentCapabilityManifest.Version,
            modelRevision,
            totalBytes = bytes.Length,
            page = McpPaging.Bytes(bytes, arguments, workspace.Revision.ToString())
        });
    }
}
