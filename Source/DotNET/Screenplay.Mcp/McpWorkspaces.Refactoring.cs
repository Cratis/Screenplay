// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object Rename(JsonElement arguments)
    {
        var workspace = Current();
        var request = new WorkspaceRenameRequest
        {
            ExpectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision")),
            ExpectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision")),
            Target = arguments.TryGetProperty("target", out var target) ? McpAstHandles.Read(target) : throw new McpFailure("Rename requires a target handle.", -32602),
            ExpectedName = McpJson.RequiredString(arguments, "expectedName"),
            NewName = McpJson.RequiredString(arguments, "newName"),
            Formatting = McpJson.Enumeration(arguments, "formatting", WorkspaceAuthoringFormatting.PreserveTrivia),
            Validation = McpJson.Enumeration(arguments, "validation", WorkspaceAuthoringValidation.Authoring)
        };
        root.Verify(workspace);
        var result = workspace.ProposeRename(request);
        return result.Accepted ? Store(new McpAuthoringProposal(workspace, result, request.Validation), arguments) : Rejected(result);
    }
}
