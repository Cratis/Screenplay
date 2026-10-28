// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object RunSpecifications(JsonElement arguments)
    {
        RefusePendingWorkspace();
        if (McpJson.OptionalString(arguments, "proposalId") is { } proposalId)
        {
            var result = SpecificationResponse(RetainedProposal(arguments).Workspace, arguments, _bodyProvider, proposalId);
            RefusePendingWorkspace();

            return result;
        }

        var persisted = new McpManagedFiles(Root).Read(McpState.FileName);
        var workspace = _workspace is not null ? Current() : persisted switch
        {
            null => OpenFromDisk(Root, Root.ApplicationName),
            _ => McpState.Deserialize(persisted).Open(Root)
        };
        Root.Verify(workspace);
        var response = SpecificationResponse(workspace, arguments, _bodyProvider);
        Root.Verify(workspace);
        new McpManagedFiles(Root).Verify(McpState.FileName, persisted);
        RefusePendingWorkspace();

        return response;
    }

    static object SpecificationResponse(ScreenplayWorkspace workspace, JsonElement arguments, ISemanticBodyExecutionProvider? provider, string? proposalId = null)
    {
        CheckSpecificationRevision(workspace, arguments);
        var report = McpSpecificationExecution.Run(workspace, McpJson.OptionalString(arguments, "specification"), McpJson.OptionalString(arguments, "scope"), provider: provider);
        var content = new
        {
            report.SourceRevision,
            report.Outcome,
            report.Discovered,
            report.Selected,
            report.Executed,
            report.Passed,
            report.Failed,
            report.Unsupported,
            executableDiagnostics = report.Diagnostics,
            report.PlanIssues,
            page = report.Outcome == "unbound" ? null : McpPaging.Page(report.Results, arguments, report.SourceRevision)
        };
        if (proposalId is null)
        {
            return McpJson.ToolResult(content, report.Outcome != "passed");
        }

        var response = JsonSerializer.SerializeToNode(content, McpJson.Options)!.AsObject();
        response["proposalId"] = JsonValue.Create(proposalId);

        return McpJson.ToolResult(response, report.Outcome != "passed");
    }

    static void CheckSpecificationRevision(ScreenplayWorkspace workspace, JsonElement arguments)
    {
        var sourceRevision = McpSourceRevision.For(workspace.Documents);
        var expected = McpJson.OptionalString(arguments, "expectedSourceRevision");
        if (McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 && expected is null)
        {
            throw new McpFailure("Subsequent source pages require expectedSourceRevision from the first page.", -32602);
        }

        if (expected is not null && expected != sourceRevision)
        {
            throw new McpFailure("StaleSourceRevision: source changed between specification pages; start a new run.") { FailureKind = "StaleSourceRevision" };
        }
    }
}
