// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Api;
using Cratis.Screenplay.Ui;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    // The HTTP API needs the executable model; the UI contract needs only the sources and adds identities and routes when the model exists.
    static object ReadPortableContractView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
    {
        var model = workspace.Compilation.Success ? workspace.Compilation.Value!.Model : null;
        if (view == "api-description" && model is null)
        {
            if (McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 || McpJson.OptionalString(arguments, "expectedModelRevision") is not null)
            {
                throw new McpFailure("StaleRevision: executable model is no longer available.");
            }

            return McpJson.ToolResult(new
            {
                workspace = McpWorkspaceTransport.Describe(workspace),
                view,
                available = false,
                executableDiagnosticsCount = workspace.Compilation.Diagnostics.Count(),
                executableDiagnosticsView = "executable-diagnostics"
            });
        }

        CheckContinuation(arguments, "expectedModelRevision", model?.Revision.ToString() ?? string.Empty, requireOnContinuation: model is not null);
        var bytes = view == "api-description" ? OpenApiDocument.Write(ApiDescription.For(model!)) : UiContractJson.Write(UiContractBuilder.For(workspace));
        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace),
            view,
            available = true,
            format = view == "api-description" ? $"openapi-{OpenApiDocument.Version}" : UiContract.Version,
            modelRevision = model?.Revision.ToString(),
            totalBytes = bytes.Length,
            page = McpPaging.Bytes(bytes, arguments, workspace.Revision.ToString())
        });
    }
}
